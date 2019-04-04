
namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// This interface can be implemented by view models, which want to be notified when
    /// the corresponding view is about to be closed.
    /// </summary>
    public interface IOnClosingHandler
    {
        /// <summary>
        /// This method is called when the corresponding view closes.
        /// </summary>
        /// <remarks>
        /// When the corresponding view is a <see cref="Window"/>, this method is called when <see cref="Window.OnClosing"/>
        /// is raised; otherwise, when <see cref="FrameworkElement.Unloaded"/> is raised.
        /// </remarks>
        /// <remarks>If you want to intercept when the corresponding window is about to be closed,
        /// use <see cref="ICancelableOnClosingHandler"/>.</remarks>
        void OnClosing();
    }

    /// <summary>
    /// This interface can be implemented by view models, which want to be notified when
    /// the corresponding window is about to be closed.
    /// </summary>
    public interface ICancelableOnClosingHandler
    {
        /// <summary>
        /// This method is called when the corresponding view's <see cref="Window.Closing"/> event was raised.
        /// </summary>
        /// <returns><see landword="true"/> the the window can be closed; otherwise, <see langword="false"/>.</returns>
        bool OnClosing();
    }
}
