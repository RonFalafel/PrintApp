using System;

namespace PrintApp.Logic.Usb
{
    public interface IUsbWriter
    {
        EventHandler<string> LineWritten { get; set; }

        void WriteCommand(string command);
    }
}