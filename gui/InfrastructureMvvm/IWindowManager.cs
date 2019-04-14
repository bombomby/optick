using System.Windows;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// Declares methods to show windows.
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Shows a window for a given view model type.
        /// </summary>
        /// <param name="owningWindow">An optional owner for the new window.</param>
        /// <typeparam name="TViewModel">The type of the view model.</typeparam>
        /// <returns>The window.</returns>
        Window ShowWindow<TViewModel>(Window owningWindow = null);

        /// <summary>
        /// Shows a window for a given view model object.
        /// </summary>
        /// <param name="viewModel">The view model for the window to be displayed.</param>
        /// <param name="owningWindow">An optional owner for the new window.</param>
        /// <returns>The window.</returns>
        Window ShowWindow(object viewModel, Window owningWindow = null);
    }
}
