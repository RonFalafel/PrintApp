using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using PrintApp.Logic.Usb;
using WebApp.Core;

namespace PrintApp.Logic
{
    public class GCodeUploader : IGCodeUploader
    {
        private readonly IPrinterWrapper _printer;
        private readonly BlockingCollection<GCodeFile> _appendingFiles;
        private bool _writerThreadIsAlive;

        public GCodeUploader(IPrinterWrapper printer)
        {
            _printer = printer;
            _appendingFiles = new BlockingCollection<GCodeFile>();
        }

        public void UploadGCode(GCodeFile file)
        {
            _appendingFiles.Add(file);
            if (!_writerThreadIsAlive)
            {
                StartFileWriterThread();
            }
        }

        private void StartFileWriterThread()
        {
            Task.Run(() =>
            {
                _writerThreadIsAlive = true;

                while (true)
                {
                    GCodeFile file = _appendingFiles.Take();
                    if (_printer.State.Status != PrinterStatus.Online)
                    {
                        _appendingFiles.Add(file); // Re-adding removed file cause the printer cant upload it now...
                        _writerThreadIsAlive = false;
                        break;
                    }

                    _printer.State.Status = PrinterStatus.Uploading;
                    _printer.WriteCommand("M28 " + file.FileName);
                    foreach (string line in file.FileLines)
                    {
                        _printer.WriteCommand(line);
                    }

                    _printer.WriteCommand("M29 " + file.FileName);
                    _printer.State.Status = PrinterStatus.Online;
                }
            });
        }
    }
}