using PrintApp.Logic.Usb;
using WebApp.Core;

namespace PrintApp.Logic
{
    public class PrinterWrapper : IPrinterWrapper
    {
        public PrinterState State { get; set; }

        private readonly IUsbWrapper _usbWrapper;

        public PrinterWrapper(IUsbWrapper usbWrapper)
        {
            _usbWrapper = usbWrapper;
            State = new PrinterState();
        }

        public void Connect()
        {
            _usbWrapper.Connect();
            State.Status = PrinterStatus.Online;
        }

        public void WriteCommand(string command)
        {
            _usbWrapper.WriteCommand(command);
        }
    }
}