//using System.Collections.Generic;
//using System.IO;
//using System.Threading.Tasks;

//namespace PrintApp.Logic
//{
//    public class MarlinPrintFileManager : IPrintFileManager
//    {
//        private MarlinPrinter _printer;

//        public MarlinPrintFileManager(MarlinPrinter printer)
//        {
//            _printer = printer;
//        }

//        public IEnumerable<string> GetPrintFiles()
//        {
//            return _printer.SdCardFileList;
//        }

//        public async Task AddFile(Stream httpFileStream, string fileName)
//        {
//            if (!_printer.WriterThread)
//                _printer.StartFileWriterThread();

//            await _printer.WriteFileToAppendingFilesList(httpFileStream, fileName);
//        }
//    }
//}
