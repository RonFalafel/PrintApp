using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PrintApp.Logic.Server
{
    public class ServerPrintFileManager : IPrintFileManager
    {
        private string _filesDirectory;

        public ServerPrintFileManager(string queueDirectory)
        {
            Directory.CreateDirectory(queueDirectory);
            _filesDirectory = queueDirectory;
        }

        public IEnumerable<string> GetPrintFiles()
        {
            return Directory.GetFiles(_filesDirectory).Select(Path.GetFileName);
        }

        // Async version (test non-async version and then delete).
        public async Task AddFile(Stream httpFileStream, string fileName)
        {
            await using var fileStream = File.Create(Path.Combine(_filesDirectory, fileName));
            await httpFileStream.CopyToAsync(fileStream);
        }

        //public void AddFile(Stream httpFileStream, string fileName)
        //{
        //    using (var fileStream = File.Create(Path.Combine(_filesDirectory, fileName)))
        //    {
        //        httpFileStream.CopyTo(fileStream);
        //    }
        //}
    }
}
