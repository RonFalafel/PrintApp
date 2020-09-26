using System;
using System.Collections.Generic;

namespace PrintApp.Logic.Mock
{
    class MockPrinter : IPrinter
    {
        public Action<string> PostPrintEvent { get; set; }
        public Action<string> PreDispatchCommandEvent { get; set; }
        public Action<string> PostReadEvent { get; set; }

        public IEnumerable<string> CommandHistory => new List<string>();

        public bool IsPrinting { get; set; } = false;

        public void CancelPrint()
        {
            IsPrinting = false;
        }

        public string GetStatus()
        {
            return !IsPrinting
                ? "Not printing!"
                : "Printing.";
        }

        public bool StartPrint(string filename)
        {
            if (!IsPrinting)
            {
                Console.WriteLine($"Started printing: {filename}");
                IsPrinting = true;
            }

            return IsPrinting;
        }

        public bool TryConnect()
        {
            return true;
        }

        public void WriteGCodeCommand(string line)
        {
            Console.WriteLine($"Command sent to printer: {line}");
        }
    }
}
