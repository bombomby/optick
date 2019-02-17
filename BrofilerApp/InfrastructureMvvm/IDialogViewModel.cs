using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// Interface for dialog view models.
    /// </summary>
    public interface IDialogViewModel
    {
        /// <summary>
        /// This event is raised when the dialog was closed.
        /// </summary>
        event EventHandler Closed;
    }
}
