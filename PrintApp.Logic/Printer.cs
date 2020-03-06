using System;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PrintApp.Logic
{
    public class Printer : IPrinter
    {
        #region Public Properties

        // To be used if we want to discard of the printing file or whatever.
        public Action<string> PostPrintEvent;

        #endregion

        #region Private Properties

        private Thread ReaderThread;
        private Semaphore _sem; // Max number of threads, gets value from bufcount.
        private SerialPort _usbPort;
        private CancellationTokenSource cts; // Cancellation token for closing the printing thread.
        private bool _isDebug, _isLog, _isProgress, _isQuit; // CMD configurations.
        private long _startTime; // Holds start time during prints.

        private Stopwatch _stopWatch;
        private long msec() { return _stopWatch.ElapsedMilliseconds; }

        private string portname = "COM7";
        private int baudrate = 115200;
        private int bufcount = 4; // nr of lines to buffer
        private int timeout = 6000;
        private long t1;
        private bool realtime = false;

        // Parameters about current print.
        private bool IsPrinting;
        private double _elapsedTime;
        private double _curPercentage;
        private double _totalTime;
        private double _remaining;

        private object _elapsedTimeLock = new object();
        private object _curPercentageLock = new object();
        private object _totalTimeLock = new object();
        private object _remainingLock = new object();

        private double ElapsedTime { get { lock (_elapsedTimeLock) { return _elapsedTime; } } set => _elapsedTime = value; }
        private double CurPercentage { get { lock (_curPercentageLock) { return _curPercentage; } } set => _curPercentage = value; }
        private double TotalTime { get { lock (_totalTimeLock) { return _totalTime; } } set => _totalTime = value; }
        private double Remaining { get { lock (_remainingLock) { return _remaining; } } set => _remaining = value; }

        #endregion

        #region Ctor

        public Printer(Action<string> PostPrintEvent = null, bool debug = false, bool log = true, bool progress = true, bool quit = false)
        {
            _isDebug = debug;
            _isLog = log;
            _isProgress = progress;
            _isQuit = quit;
            _startTime = 0;
            _stopWatch = Stopwatch.StartNew();
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

            try
            {
                // open port and wait for printer to boot
                _usbPort = new SerialPort(portname, baudrate);
                _usbPort.Open();
                _usbPort.NewLine = "\n";
                _usbPort.DtrEnable = true;
                _usbPort.RtsEnable = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("SENDG: Cannot open serial port (Portname=" + portname + ")");
                if (_isDebug)
                    Console.WriteLine(ex.ToString());
                return false;
            }

            // TODO: Is this really needed?
            Thread.Sleep(2000);

            // Init semaphore and Start 2nd thread
            _sem = new Semaphore(0, bufcount);
            _sem.Release(bufcount);
            cts = new CancellationTokenSource();
            ReaderThread = new Thread(() => Reader(cts.Token));
            _startTime = msec();
            ReaderThread.Start();
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
                    string l = Regex.Replace(line, @"[;(]+.*[\n)]*", "");
                    l = l.Trim();
                    if (l.Length > 0)
                    {
                        linenr++;
                        line = l + "\n";
                        _sem.WaitOne();
                        _usbPort.Write(line);
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
                PostPrintEvent?.Invoke(filename);
                Console.WriteLine("Total time: " + e + " msec");
            });

            return true;
        }

        #endregion

        #region Private Methods

        /*
         * Reader thread
         * Read data and find EOL chars
         * When one is found: the line is displayed
         */
        private void Reader(CancellationToken token)
        {
            string s = "", n = "";
            long recnr = 0, t1 = 0;
            try
            {
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
                                _sem.Release(1);
                                recnr++;
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
            catch
            {
            }
            finally
            {
                if (!_isQuit)
                    Console.WriteLine("Some error during read");
            }
        }

        #endregion
    }
}
