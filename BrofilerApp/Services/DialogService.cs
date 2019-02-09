using System.Windows;


namespace Profiler.Services
{
    class DialogService : IDialogService
    {
        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
