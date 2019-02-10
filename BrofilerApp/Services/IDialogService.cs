
namespace Profiler.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message);   
        string FilePath { get; set; }   
        bool OpenFileDialog();
        bool OpenFolderDialog();
        bool SaveFileDialog();
        bool SaveFileDialog(string defaultFileName, string defaultExt, string initialDirectory = null);
    }
}
