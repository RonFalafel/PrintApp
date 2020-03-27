using System.IO.Ports;

namespace PrintApp.Logic.Usb
{
    public class UsbWrapper : IUsbWrapper
    {
        private readonly SerialPort _usbPort;
        private readonly IUsbReader _usbReader;
        private readonly IUsbWriter _usbWriter;

        public UsbWrapper(SerialPort usbPort, IUsbReader usbReader, IUsbWriter usbWriter)
        {
            _usbPort = usbPort;
            _usbReader = usbReader;
            _usbWriter = usbWriter;
        }

        public void Connect()
        {
            _usbPort.Open();
            _usbPort.ReadTimeout = SerialPort.InfiniteTimeout;
            StartReader();
        }

        public void Dispose()
        {
            _usbPort.Close();
        }

        public void WriteCommand(string command)
        {
            _usbWriter.WriteCommand(command);
        }

        private void StartReader()
        {
            _usbReader.StartReader();
        }
    }
}
