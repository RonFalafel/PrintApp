//using System;
//using System.Collections.Generic;

//namespace PrintApp.Logic
//{
//    public interface IPrinter
//    {
//        Action<string> PostPrintEvent { get; set; }

//        Action<string> PreDispatchCommandEvent { get; set; }

//        Action<string> PostReadEvent { get; set; }

//        IEnumerable<string> CommandHistory { get; }

//        bool IsPrinting { get; }

//        bool StartPrint(string filename);

//        string GetStatus();

//        void CancelPrint();

//        bool TryConnect();

//        void WriteGCodeCommand(string line);
//    }
//}
