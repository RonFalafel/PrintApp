using System;
using System.Collections.Generic;
using System.IO;
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

            MarlinPrinter printer = new MarlinPrinter(settings);
            if(!printer.TryConnect())
                return false;

            Printers.Add(settings.PrinterName, printer);
            CurrentPrinterName = settings.PrinterName;
            InitializeQueue(settings.PrinterName, printer);
            return true;
        }

        /// <summary>
        /// Creates the printer folders (configs, queue, printed...) if they don't already exist.
        /// Adds them to the printer's queue.
        /// </summary>
        private void InitializeQueue(string printerName, MarlinPrinter printer)
        {
            // Initialization of marlin queue:
            MarlinPrintFileManager printFileManager = new MarlinPrintFileManager(printer);
            Queues.Add(printerName, printFileManager);
        }
    }
}
