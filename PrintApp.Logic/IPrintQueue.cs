using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PrintApp.Logic
{
    public interface IPrintQueue
    {
        string QueueDirectory { get; set; }

        string PrintedDirectory { get; set; }

        bool IsEmpty();

        string GetNextFile();

        void CutFileFromQueueToPrinted(string filePath);

        string GetAfterPrintedPath(string filePath);

        IEnumerable<string> GetQueueFiles();

        Task AddFile(Stream httpFileStream, string fileName);
    }
}
