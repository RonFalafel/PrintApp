using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace PrintApp.Logic.Usb
{
    public class UsbReader : IUsbReader
    {
        public EventHandler<string> LineReceived { get; set; }

        private readonly SerialPort _usbPort;

        public UsbReader(SerialPort usbPort)
        {
            _usbPort = usbPort;
        }

        public void StartReader()
        {
            Task.Run(() =>
            {
                string buffer = "";

                while (true)
                {
                    buffer += ((char)_usbPort.ReadChar()).ToString();
                    while (_usbPort.BytesToRead > 0)
                    {
                        buffer += _usbPort.ReadExisting();
                    }

                    while (buffer.Contains("\n"))
                    {
                        int p = buffer.IndexOf("\n", StringComparison.Ordinal);
                        string line = buffer.Substring(0, p);
                        buffer = buffer.Substring(p + 1, buffer.Length - p - 1);
                        if (line.Length > 0)
                        {
                            LineReceived?.Invoke(this, line);
                        }
                    }
                }
            });
        }
    }
}