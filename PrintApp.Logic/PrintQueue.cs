using System.IO;
using System.Linq;

namespace PrintApp.Logic
{
    public class PrintQueue : IPrintQueue
    {
        private string _filesDirectory;
        private string _queueDirectory;
        private string _printedDirectory;

        public PrintQueue(string filesDirectory, string queueDirectory, string printedDirectory)
        {
            _filesDirectory = filesDirectory;
            _queueDirectory = queueDirectory;
            _printedDirectory = printedDirectory;
        }

        public bool IsEmpty()
        {
            return !Directory.GetFiles(_queueDirectory).Any();
        }

        public string GetNextFile()
        {
            return Directory.GetFiles(_queueDirectory)[0];
        }

        public void CutFileFromQueueToPrinted(string filePath)
        {
            File.Copy(filePath, GetAfterPrintedPath(filePath), true);
            File.Delete(filePath);
        }

        public string GetAfterPrintedPath(string filePath)
        {
            return Path.Combine(_printedDirectory, Path.GetFileName(filePath));
        }

        public void PostFile(Stream httpFileStream, string fileName)
        {
            using (var fileStream = File.Create(Path.Combine(_queueDirectory, fileName)))
            {
                httpFileStream.Seek(0, SeekOrigin.Begin);
                httpFileStream.CopyTo(fileStream);
            }
        }
    }
}
