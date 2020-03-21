using System;
using System.Collections.Generic;
using System.Text;

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
