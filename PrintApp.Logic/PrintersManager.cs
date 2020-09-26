using System;
using System.Collections.Generic;
using PrintApp.Logic.Marlin;
using PrintApp.Logic.Server;
using PrintApp.Logic.Mock;
using WebApp.Core;

namespace PrintApp.Logic
{
    public class PrintersManager
    {
        public Dictionary<string, IPrinter> Printers;

        public Dictionary<string, IPrintFileManager> Queues;

        public string CurrentPrinterName 
        { 
            get => _currentPrinterName;
            set
            {
                PrinterStateHasChanged(this, null);
                _currentPrinterName = value;
            } 
        }

        public IPrinter CurrentPrinter => Printers[CurrentPrinterName];

        public IPrintFileManager CurrentQueue => Queues[CurrentPrinterName];

        public event EventHandler PrinterStateHasChanged;

        private string _currentPrinterName;

        public PrintersManager()
        {
            Printers = new Dictionary<string, IPrinter>();
            Queues = new Dictionary<string, IPrintFileManager>();
        }

        public bool TryAddPrinter(PrinterConnectionSettings settings)
        {
            if (Printers.ContainsKey(settings.PrinterName))
                return false;

            switch (settings.ConnectionType)
            {
                case ConnectionTypes.Marlin:
                    var marlinPrinter = new MarlinPrinter(settings);
                    if (!marlinPrinter.TryConnect()) return false;
                    Printers.Add(settings.PrinterName, marlinPrinter);
                    CurrentPrinterName = settings.PrinterName;
                    var marlinPrintFileManager = new MarlinPrintFileManager(marlinPrinter);
                    Queues.Add(settings.PrinterName, marlinPrintFileManager);
                    return true;
                case ConnectionTypes.Server:
                    var serverPrinter = new ServerPrinter(settings);
                    if (!serverPrinter.TryConnect()) return false;
                    var serverPrintFileManager = new ServerPrintFileManager($@"{settings.PrinterName}\\ToPrint"); // TODO: Get path from config.
                    Printers.Add(settings.PrinterName, serverPrinter);
                    CurrentPrinterName = settings.PrinterName;
                    Queues.Add(settings.PrinterName, serverPrintFileManager);
                    return true;
                case ConnectionTypes.Mock:
                    var mockPrinter = new MockPrinter();
                    if (!mockPrinter.TryConnect()) return false;
                    var mockServerPrintFileManager = new ServerPrintFileManager($@"{settings.PrinterName}\\ToPrint"); // TODO: Get path from config.
                    Printers.Add(settings.PrinterName, mockPrinter);
                    CurrentPrinterName = settings.PrinterName;
                    Queues.Add(settings.PrinterName, mockServerPrintFileManager);
                    return true;
                default:
                    return false; // TODO: Write log that printer type is not supported.
            }
        }
    }
}
