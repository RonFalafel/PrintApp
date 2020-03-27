using WebApp.Core;

namespace PrintApp.Logic
{
    public interface IGCodeUploader
    {
        void UploadGCode(GCodeFile file);
    }
}