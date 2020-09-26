using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading.Tasks;
using WebApp.Core;

namespace PrintApp.Logic.Marlin
{
    public class MarlinPrinter : IPrinter
    {
        #region Public Properties

        // To be used if we want to discard of the printing file or whatever.
        public Action<string> PostPrintEvent { get; set; }

        // To be used if we want to log the sent command or whatever.
        public Action<string> PreDispatchCommandEvent { get; set; }

        // To be used if we want to log the data coming from the printer or whatever.
        public Action<string> PostReadEvent { get; set; }

        public IEnumerable<string> CommandHistory => _commandHistory;

        public bool IsPrinting { get; private set; }

        public IEnumerable<string> SdCardFileList { get; set; }

        public PrinterStatus Status { get; private set; }

        public bool WriterThread { get; private set; }

        #endregion

        #region Private Properties

        private SerialPort _usbPort;
        private int _connectionTimeout; // The timeout to be used when attempting to connect to USB port.
        private string _portname;
        private int _baudrate;

        private bool _isReading;
        private Queue<string> _commandHistory;

        private BlockingCollection<GCodeFile> _appendingFiles;

        #endregion

        #region Ctor

        public MarlinPrinter(PrinterConnectionSettings connectionSettings, int connectionTimeout = 50000)
        {
            _connectionTimeout = connectionTimeout;
            _portname = connectionSettings.UsbPort;
            _baudrate = connectionSettings.Baudrate;
            _commandHistory = new Queue<string>();
            _appendingFiles = new BlockingCollection<GCodeFile>();
            SdCardFileList = new List<string>();
            WriterThread = false;
        }

        public MarlinPrinter(int connectionTimeout = 50000)
        {
            _connectionTimeout = connectionTimeout;
            _commandHistory = new Queue<string>();
            _appendingFiles = new BlockingCollection<GCodeFile>();
            SdCardFileList = new List<string>();
            WriterThread = false;
        }

        #endregion

        #region Public Methods

        public string GetStatus()
        {
            return !IsPrinting
                ? "Not printing!"
                : "Printing...";
        }

        public void CancelPrint()
        {
            WriteGCodeCommand($"M999");
        }

        public bool TryConnect()
        {
            if (IsPrinting)
                return false;

            try
            {
                // open port and wait for printer to boot
                _usbPort = new SerialPort(_portname, _baudrate);
                _usbPort.ReadTimeout = _connectionTimeout;
                _usbPort.Open();
                _usbPort.NewLine = "\n";
                _usbPort.DtrEnable = true;
                _usbPort.RtsEnable = true;
                _usbPort.Write("M105");

                char[] readInfo = new char[100];
                _usbPort.Read(readInfo, 0, 100);
            }
            catch (Exception ex) // todo: log this or something...
            {
                return false;
            }

            // Setting the read timeout to infinity so we don't time out while waiting for updates from the printer.
            _usbPort.ReadTimeout = SerialPort.InfiniteTimeout;
            StartReaderThread();
            StartFileWriterThread();
            Status = PrinterStatus.Online;
            return true;
        }

        public bool StartPrint(string filename)
        {
            if (Status != PrinterStatus.Online)
                return false;

            Status = PrinterStatus.Printing;
            WriteGCodeCommand($"M23 {filename}");
            WriteGCodeCommand($"M24");
            IsPrinting = false;
            return true;
        }

        public void WriteGCodeCommand(string line)
        {
            logCommand("Write: " + line);
            PreDispatchCommandEvent?.Invoke(line);
            _usbPort.Write(line + "\n");
        }

        public async Task WriteFileToSd(Stream fileStream, string fileName)
        {
            _usbPort.Write("M28 " + fileName + "\n");

            logCommand("Write: Writing File...");
            PreDispatchCommandEvent?.Invoke("Write: Writing File...");
            using (StreamReader binaryReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = await binaryReader.ReadLineAsync()) != null)
                {
                     _usbPort.Write(line + "\n");
                }
            }

           _usbPort.Write("M29 " + fileName + "\n");
        }

        public async Task WriteFileToAppendingFilesList(Stream fileStream, string fileName)
        {
            using (StreamReader binaryReader = new StreamReader(fileStream))
            {
                List<string> fileLines = new List<string>();
                string line;
                while ((line = await binaryReader.ReadLineAsync()) != null)
                {
                    fileLines.Add(line);
                }

                _appendingFiles.Add(new GCodeFile(fileName, fileLines));
            }
        }

        public void StartFileWriterThread()
        {
            Task.Run(() =>
            {
                WriterThread = true;

                while (true)
                {
                    GCodeFile file = _appendingFiles.Take();
                    if (Status != PrinterStatus.Online)
                    {
                        _appendingFiles.Add(file); // Re-adding removed file cause the printer cant upload it now...
                        WriterThread = false;
                        break;
                    }

                    Status = PrinterStatus.Uploading;
                    _usbPort.Write("M28 " + file.FileName + "\n");
                    logCommand("Write: Writing File...");
                    PreDispatchCommandEvent?.Invoke("Write: Writing File...");
                    foreach (string line in file.FileLines)
                    {
                        _usbPort.Write(line + "\n"); // If we want to log these to the terminal and such, just call WriteGCodeCommand instead.
                    }

                    _usbPort.Write("M29 " + file.FileName + "\n");
                    Status = PrinterStatus.Online;
                }
            });

        }

        #endregion

        #region Private Methods

        private void logCommand(string command)
        {
            _commandHistory.Enqueue(command);

            while (_commandHistory.Count > 50) // Make configurable.
            {
                _commandHistory.Dequeue();
            }
        }

        /// <summary>
        /// Starts the reader thread.
        /// </summary>
        private void StartReaderThread()
        {
            _isReading = true;

            Task.Run(() =>
            {
                string s = "", n = "";
                List<string> linesRead = null;

                while (_isReading)
                {
                    n += ((char)_usbPort.ReadChar()).ToString();
                    while (_usbPort.BytesToRead > 0)
                    {
                        n += _usbPort.ReadExisting();
                    }

                    while (n.Contains("\n"))
                    {
                        int p = n.IndexOf("\n");
                        s = n.Substring(0, p);
                        n = n.Substring(p + 1, n.Length - p - 1);
                        if (s.Length > 0)
                        {
                            logCommand("Read lines: " + s);
                            PostReadEvent?.Invoke(s);
                        }

                        if (s == "Done printing file")
                        {
                            Status = PrinterStatus.Online;
                            StartFileWriterThread();
                            PostPrintEvent?.Invoke(""); // Maybe find a way to make this not need the string?
                        }

                        if (s == "Begin file list")
                        {
                            linesRead = new List<string>();
                        }
                        else if (linesRead != null && s == "End file list")
                        {
                            SdCardFileList = linesRead;
                            linesRead = null;
                        }
                        else if (linesRead != null)
                            linesRead.Add(s);
                    }
                }
            });
        }

        #endregion
    }
}
