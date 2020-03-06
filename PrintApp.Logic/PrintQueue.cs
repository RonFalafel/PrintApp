using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrintApp.Logic
{
    public class PrintQueue : IPrintQueue
    {
        public string QueueDirectory { get; set; }
        public string PrintedDirectory { get; set; }

        public PrintQueue(string queueDirectory, string printedDirectory)
        {
            QueueDirectory = queueDirectory;
            PrintedDirectory = printedDirectory;
        }

        public bool IsEmpty()
        {
            return !Directory.GetFiles(QueueDirectory).Any();
        }

        public string GetNextFile()
        {
            return Directory.GetFiles(QueueDirectory)[0];
        }

        public void CutFileFromQueueToPrinted(string filePath)
        {
            File.Copy(filePath, GetAfterPrintedPath(filePath), true);
            File.Delete(filePath);
        }

        public string GetAfterPrintedPath(string filePath)
        {
            return Path.Combine(PrintedDirectory, Path.GetFileName(filePath));
        }

        public IEnumerable<string> GetQueueFiles()
        {
            return Directory.GetFiles(QueueDirectory).Select((filePath) => Path.GetFileName(filePath));
        }

        public async Task AddFile(Stream httpFileStream, string fileName)
        {
            using (var fileStream = File.Create(Path.Combine(QueueDirectory, fileName)))
            {
                // httpFileStream.Seek(0, SeekOrigin.Begin);
                await httpFileStream.CopyToAsync(fileStream);
            }
        }
    }
}
