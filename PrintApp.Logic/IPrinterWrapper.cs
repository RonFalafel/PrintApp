using WebApp.Core;

namespace PrintApp.Logic
{
    public interface IPrinterWrapper
    {
        PrinterState State { get; set; }

        void Connect();

        void WriteCommand(string command);
    }
}