using System;
using System.Collections.Generic;
using System.IO.Ports;
using PrintApp.Logic.Usb;
using WebApp.Core;

namespace PrintApp.Logic
{
    public class PrintersManager
    {
        public Dictionary<string, IPrinterWrapper> Printers;

        public Dictionary<string, IGCodeUploader> GCodeUploaders;

        public string CurrentPrinterName 
        { 
            get => _currentPrinterName;
            set
            {
                PrinterStateHasChanged?.Invoke(this, null);
                FileListReceived?.Invoke(this, null);
                _currentPrinterName = value;
            } 
        }

        public IPrinterWrapper CurrentPrinter => Printers[CurrentPrinterName];

        public IGCodeUploader CurrentQueue => GCodeUploaders[CurrentPrinterName];

        public event EventHandler PrinterStateHasChanged;

        public event EventHandler FileListReceived;

        private string _currentPrinterName;

        public PrintersManager()
        {
            Printers = new Dictionary<string, IPrinterWrapper>();
            GCodeUploaders = new Dictionary<string, IGCodeUploader>();
        }

        public void AddPrinter(PrinterConnectionSettings settings)
        {
            SerialPort usbPort = new SerialPort(settings.UsbPort, settings.Baudrate)
            {
                ReadTimeout = 5000, // Make configurable.
                NewLine = "\n", // Marlin new-line
                DtrEnable = true,
                RtsEnable = true
            };

            UsbReader reader = new UsbReader(usbPort);
            UsbWriter writer = new UsbWriter(usbPort);
            UsbWrapper usb = new UsbWrapper(usbPort, reader, writer);
            PrinterWrapper printer = new PrinterWrapper(usb);
            reader.LineReceived += (sender, line) => printer.State.CommandHistory.Enqueue(line);
            writer.LineWritten += (sender, line) => printer.State.CommandHistory.Enqueue($">>> {line}");

            // todo: find a prettier way to update the ui when the state changes.
            reader.LineReceived += (sender, line) => PrinterStateHasChanged?.Invoke(null, null);
            writer.LineWritten += (sender, line) => PrinterStateHasChanged?.Invoke(null, null);

            MarlinStateUpdater stateUpdater = new MarlinStateUpdater(printer.State);
            reader.LineReceived += (sender, line) => stateUpdater.ReadMarlinResponse(line);
            stateUpdater.DoneReadingFileList += (sender, args) => FileListReceived(sender, args);

            printer.Connect(); // todo: try-catch here? return false on failed?

            Printers.Add(settings.PrinterName, printer);
            GCodeUploaders.Add(settings.PrinterName, new GCodeUploader(printer));
            CurrentPrinterName = settings.PrinterName;
        }
    }
}
