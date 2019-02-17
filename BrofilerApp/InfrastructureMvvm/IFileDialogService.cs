
namespace Profiler.InfrastructureMvvm
{
    public interface IFileDialogService
    {
        void ShowMessage(string message);   
        string FilePath { get; set; }   
        bool OpenFileDialog();
        bool OpenFolderDialog();
        bool SaveFileDialog();
        bool SaveFileDialog(string defaultFileName, string defaultExt, string initialDirectory = null);
    }
}
