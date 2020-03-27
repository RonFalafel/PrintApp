using System;

namespace PrintApp.Logic.Usb
{
    public interface IUsbReader
    {
        EventHandler<string> LineReceived { get; set; }

        void StartReader();
    }
}