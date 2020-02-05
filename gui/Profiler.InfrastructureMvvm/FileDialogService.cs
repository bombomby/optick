using Microsoft.Win32;
using System.Windows;


namespace Profiler.InfrastructureMvvm
{
    public class FileDialogService : IFileDialogService
    {
        public string FilePath { get; set; }

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                FilePath = openFileDialog.FileName;
                return true;
            }
            return false;
        }

        public bool OpenFolderDialog()
        {
            using (var folderDialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                folderDialog.SelectedPath = System.AppDomain.CurrentDomain.BaseDirectory;
                folderDialog.Description = "Select destination folder";
                if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    FilePath = folderDialog.SelectedPath;
                    return true;
                }
                return false;
            }
        }

        public bool SaveFileDialog()
        {
            return SaveFileDialog(null,null,null,null);
        }

        public bool SaveFileDialog(string defaultFileName, string defaultExt, string filter = null, string initialDirectory=null)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = initialDirectory != null ? initialDirectory: System.AppDomain.CurrentDomain.BaseDirectory;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.Title = @"Select save location file name";
            saveFileDialog.DefaultExt = defaultExt;
            saveFileDialog.Filter = filter;
            saveFileDialog.AddExtension = true;
            saveFileDialog.FileName = defaultFileName;
            saveFileDialog.RestoreDirectory = true;

            if (saveFileDialog.ShowDialog() == true)
            {
                FilePath = saveFileDialog.FileName;
                return true;
            }
            return false;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
