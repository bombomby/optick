using System.Windows;

namespace Profiler.InfrastructureMvvm
{
    public class WindowManager:IWindowManager
    {
        public Window ShowWindow<TViewModel>(Window owningWindow = null)
        {
            var window = (Window)ViewLocator.GetViewForViewModel<TViewModel>();
            window.Owner = owningWindow;
            window.Show();
            return window;
        }

        public Window ShowWindow(object viewModel, Window owningWindow = null)
        {
            var window = (Window)ViewLocator.GetViewForViewModel(viewModel);
            window.Owner = owningWindow;
            window.Show();
            return window;
        }
    }
}
