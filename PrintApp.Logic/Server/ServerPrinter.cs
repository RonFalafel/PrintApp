using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using WebApp.Core;

namespace PrintApp.Logic.Server
{
    /// <summary>
    /// Manages the printer only via direct real-time GCODE commands.
    /// </summary>
    public class ServerPrinter : IPrinter
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

        #endregion

        #region Private Properties

        private long msec() { return _stopWatch.ElapsedMilliseconds; }
        private double ElapsedTime { get { lock (_elapsedTimeLock) { return _elapsedTime; } } set => _elapsedTime = value; }
        private double CurPercentage { get { lock (_curPercentageLock) { return _curPercentage; } } set => _curPercentage = value; }
        private double TotalTime { get { lock (_totalTimeLock) { return _totalTime; } } set => _totalTime = value; }
        private double Remaining { get { lock (_remainingLock) { return _remaining; } } set => _remaining = value; }

        private Thread ReaderThread;
        private Semaphore _sem; // Max number of threads, gets value from bufcount.
        private SerialPort _usbPort;
        private CancellationTokenSource cts; // Cancellation token for closing the printing thread.
        private bool _isDebug, _isLog, _isProgress, _isQuit; // CMD configurations.
        private long _startTime; // Holds start time during prints.
        private Stopwatch _stopWatch;
        private int _connectionTimeout; // The timeout to be used when attempting to connect to USB port.
        private string portname;
        private int baudrate;
        private int bufcount = 4; // nr of lines to buffer
        private int timeout = 6000;
        private long t1;
        private bool realtime = false;

        // Parameters about current print.
        private double _elapsedTime;
        private double _curPercentage;
        private double _totalTime;
        private double _remaining;

        private object _elapsedTimeLock = new object();
        private object _curPercentageLock = new object();
        private object _totalTimeLock = new object();
        private object _remainingLock = new object();

        private IList<string> _commandHistory;

        #endregion

        #region Ctor

        public ServerPrinter(PrinterConnectionSettings connectionSettings, int connectionTimeout = 50000, Action<string> postPrintEvent = null, bool debug = false, bool log = true, bool progress = true, bool quit = false)
        {
            PostPrintEvent = postPrintEvent;
            _connectionTimeout = connectionTimeout;
            portname = connectionSettings.UsbPort;
            baudrate = connectionSettings.Baudrate;
            _isDebug = debug;
            _isLog = log;
            _isProgress = progress;
            _isQuit = quit;
            _startTime = 0;
            _stopWatch = Stopwatch.StartNew();
            _commandHistory = new List<string>();
        }

        public ServerPrinter(int connectionTimeout = 50000, Action<string> postPrintEvent = null, bool debug = false, bool log = true, bool progress = true, bool quit = false)
        {
            PostPrintEvent = postPrintEvent;
            _connectionTimeout = connectionTimeout;
            _isDebug = debug;
            _isLog = log;
            _isProgress = progress;
            _isQuit = quit;
            _startTime = 0;
            _stopWatch = Stopwatch.StartNew();
            _commandHistory = new List<string>();
        }

        #endregion

        #region Public Methods

        public string GetStatus()
        {
            return !IsPrinting
                ? "Not printing!"
                : $"Elapsed Time: {ElapsedTime} minutes, Percentage: {CurPercentage}%, Remaining: {Remaining} minutes.";
        }

        public void CancelPrint()
        {
            // Wait for the last line to complete (1sec fixed time) and abort thread
            for (int i = 0; i < bufcount - 1; i++)
                _sem.WaitOne();

            long e = msec() - _startTime;
            _isQuit = true;
            Thread.Sleep(timeout);
            _usbPort.Close();
            // Request cancellation.
            cts.Cancel();
            Console.WriteLine("Cancellation set in token source...");
            Thread.Sleep(2500);
            // Cancellation should have happened, so call Dispose.
            cts.Dispose();
            IsPrinting = false;
            Console.WriteLine("Total time: " + e + " msec");
        }

        public bool TryConnect()
        {
            if (IsPrinting)
                return true;

            if (_isDebug)
                Console.WriteLine("Testing connection to printer (Portname=" + portname + ").");

            try
            {
                // open port and wait for printer to boot
                _usbPort = new SerialPort(portname, baudrate);
                _usbPort.ReadTimeout = _connectionTimeout;
                _usbPort.Open();
                _usbPort.NewLine = "\n";
                _usbPort.DtrEnable = true;
                _usbPort.RtsEnable = true;
                _usbPort.Write("M105");

                char[] readInfo = new char[100];
                Console.WriteLine(_usbPort.Read(readInfo, 0, 100) + " bytes read from usb port.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Cannot open serial port (Portname=" + portname + ").");
                if (_isDebug)
                    Console.WriteLine(ex.ToString());
                return false;
            }

            // Setting the read timeout to infinity so we don't time out while waiting for updates from the printer.
            _usbPort.ReadTimeout = SerialPort.InfiniteTimeout;
            cts = new CancellationTokenSource();
            ReaderThread = new Thread(() => Reader(cts.Token));
            _startTime = msec();
            ReaderThread.Start();

            return true;
        }

        public bool StartPrint(string filename)
        {
            if (IsPrinting)
                return false;

            if (_isDebug)
            {
                Console.WriteLine("Port: " + portname + " " + baudrate + "bps");
                Console.WriteLine("File: " + filename + " buffer:" + bufcount);
                Console.WriteLine("Realtime priority: " + (realtime ? "ENABLED" : "DISABLED"));
            }

            if (realtime)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    p.PriorityClass = ProcessPriorityClass.RealTime;
                }
            }

            // Init semaphore and Start 2nd thread
            _sem = new Semaphore(0, bufcount);
            _sem.Release(bufcount);
            
            StreamReader reader;

            // Send all lines in the file
            string line;
            int linenr = 1;
            _startTime = msec();

            try
            {
                reader = new StreamReader(filename);
                IsPrinting = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SENDG: Cannot open file! (filename=" + filename + ")");
                if (_isDebug) Console.WriteLine(ex.ToString());
                // Request cancellation.
                cts.Cancel();
                Console.WriteLine("Cancellation set in token source...");
                Thread.Sleep(2500);
                // Cancellation should have happened, so call Dispose.
                cts.Dispose();
                return false;
            }

            Task.Run(() =>
            {
                while ((line = reader.ReadLine()) != null)
                {
                    line = Regex.Replace(line, @"[;(]+.*[\n)]*", "").Trim();

                    if (line.Length > 0)
                    {
                        linenr++;
                        _sem.WaitOne();
                        WriteGCodeCommand(line);
                        // 20 min, 10%, total = 2min/%, total = 200 min
                        t1 = msec();
                        ElapsedTime = (t1 - _startTime) / 60000.0; // elapsed time in minutes
                        CurPercentage = (100.0 * reader.BaseStream.Position / reader.BaseStream.Length); // current percentage
                        TotalTime = 100.0 * (ElapsedTime / CurPercentage); // remaining time in min
                        ElapsedTime = Math.Round(ElapsedTime);
                        TotalTime = Math.Round(TotalTime);
                        Remaining = TotalTime - ElapsedTime;
                        CurPercentage = Math.Floor(CurPercentage);
                        if (_isProgress)
                        {
                            Console.WriteLine(ElapsedTime + "min: Line " + linenr + " (" + CurPercentage + "%) Remaining=" + Remaining + "min, Total=" + (TotalTime) + "min");
                        }
                        if (_isLog)
                        {
                            Console.WriteLine((t1 - _startTime) + " " + linenr + " > " + line);
                        }
                    }
                }

                // Wait for the last line to complete (1sec fixed time) and abort thread
                for (int i = 0; i < bufcount - 1; i++)
                    _sem.WaitOne();

                long totalTime = msec() - _startTime;
                //_isQuit = true;
                //Thread.Sleep(timeout);
                //_usbPort.Close();
                // Request cancellation.
                //cts.Cancel();
                //Console.WriteLine("Cancellation set in token source...");
                //Thread.Sleep(2500);
                //// Cancellation should have happened, so call Dispose.
                //cts.Dispose();
                IsPrinting = false;
                PostPrintEvent?.Invoke(filename);
                Console.WriteLine("Total time: " + totalTime + " msec");
            });

            return true;
        }

        public void WriteGCodeCommand(string line)
        {
            _commandHistory.Add("Write: " + line);
            PreDispatchCommandEvent?.Invoke(line);
            _usbPort.Write(line + "\n");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Reader thread.
        /// Reads data and finds EOL chars.
        /// When one is found: the line is displayed.
        /// </summary>
        /// <param name="token">The token to be used for cancelling the print mid-print.</param>
        private void Reader(CancellationToken token)
        {
            string s = "", n = "";
            long recnr = 0, t1 = 0;
            while (true)
            {
                if (token.IsCancellationRequested)
                    break;
                n += ((char)_usbPort.ReadChar()).ToString();
                while (_usbPort.BytesToRead > 0)
                {
                    n += _usbPort.ReadExisting();
                }

                while (n.Contains("\n"))
                {
                    t1 = msec();
                    int p = n.IndexOf("\n");
                    s = n.Substring(0, p);
                    n = n.Substring(p + 1, n.Length - p - 1);
                    if (s.Length > 0)
                    {
                        try
                        {
                            if (IsPrinting) _sem.Release(1);
                            recnr++;
                            _commandHistory.Add("Read: " + s);
                            PostReadEvent?.Invoke(s);
                            if (_isDebug) Console.WriteLine((t1 - _startTime) + " " + recnr + " < " + s);
                        }
                        catch
                        {
                            // unexpected data?
                            if (_isDebug) Console.WriteLine((t1 - _startTime) + " " + recnr + " << " + s);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
