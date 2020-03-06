using System.IO;

namespace PrintApp.Logic
{
    public interface IPrintQueue
    {
        bool IsEmpty();

        string GetNextFile();

        void CutFileFromQueueToPrinted(string filePath);

        string GetAfterPrintedPath(string filePath);

        void PostFile(Stream fileStream, string fileName);
    }
}
