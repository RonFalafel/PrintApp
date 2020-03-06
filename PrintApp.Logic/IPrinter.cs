namespace PrintApp.Logic
{
    public interface IPrinter
    {
        bool StartPrint(string filename);

        string GetStatus();

        void CancelPrint();
    }
}
