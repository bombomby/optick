using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// Provides an method to execute an action in the dispatcher thread.
    /// </summary>
    public interface IUiExecution
    {
        /// <summary>
        /// Executes the passed action in the dispatcher thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        void Execute(Action action);

        /// <summary>
        /// Executes the passed action in the dispatcher thread asynchronously.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        Task ExecuteAsync(Action action);
    }
}
