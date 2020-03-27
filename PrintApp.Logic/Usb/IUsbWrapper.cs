using System;

namespace PrintApp.Logic.Usb
{
    public interface IUsbWrapper : IDisposable
    {
        void Connect();

        void WriteCommand(string command);
    }
}