using System.Collections.Generic;

namespace WebApp.Core
{
    public class PrinterState // todo: temps, x, y, z...
    {
        public PrinterStatus Status;

        public List<string> GCodes;

        public FixedSizedQueue<string> CommandHistory;

        public PrinterState(PrinterStatus status = PrinterStatus.Offline)
        {
            Status = status;
            GCodes = new List<string>();
            CommandHistory = new FixedSizedQueue<string>();
        }
    }
}