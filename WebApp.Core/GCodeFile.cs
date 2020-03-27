using System.Collections.Generic;

namespace WebApp.Core
{
    public class GCodeFile
    {
        public string FileName;

        public IEnumerable<string> FileLines;

        public GCodeFile(string fileName, IEnumerable<string> fileLines)
        {
            FileName = fileName;
            FileLines = fileLines;
        }
    }
}
