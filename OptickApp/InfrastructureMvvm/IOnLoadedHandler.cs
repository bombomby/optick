using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// This interface can be implemented by view models, which want to be notified when
    /// the corresponding view was loaded.
    /// </summary>
    public interface IOnLoadedHandler
    {
        /// <summary>
        /// This method is called when the corresponding view's <see cref="Window.Closing"/> or
        /// <see cref="FrameworkElement.Unloaded"/> event was raised.
        /// </summary>
        /// <returns></returns>
        Task OnLoadedAsync();
    }
}
