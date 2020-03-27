using System;
using System.IO.Ports;

namespace PrintApp.Logic.Usb
{
    public class UsbWriter : IUsbWriter
    {
        public EventHandler<string> LineWritten { get; set; }

        private readonly SerialPort _usbPort;

        public UsbWriter(SerialPort usbPort)
        {
            _usbPort = usbPort;
        }

        public void WriteCommand(string command)
        {
            _usbPort.WriteLine(command);
            LineWritten?.Invoke(this, command);
        }
    }
}