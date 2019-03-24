using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// Base class for dialog view models without a specific result.
    /// </summary>
    public abstract class DialogViewModel : BaseViewModel, IDialogViewModel
    {
        private readonly TaskCompletionSource<int> _tcs;

        protected DialogViewModel()
        {
            _tcs = new TaskCompletionSource<int>();
        }

        /// <summary>
        /// Completes the <see cref="DialogViewModel.Task"/> and raises the <see cref="Closed"/> event.
        /// </summary>
        protected void Close()
        {
            _tcs.SetResult(0);

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// A task promising the closing the dialog view model. It is completed when <see cref="Close()"/> was called.
        /// </summary>
        public Task Task => _tcs.Task;

        /// <summary>
        /// This event is raised when the dialog was closed.
        /// </summary>
        public event EventHandler Closed;
    }

    /// <summary>
    /// Base class for dialog view models returning a result.
    /// </summary>
    public abstract class DialogViewModel<TResult> : BaseViewModel, IDialogViewModel
    {
        private readonly TaskCompletionSource<TResult> _tcs;

        protected DialogViewModel()
        {
            _tcs = new TaskCompletionSource<TResult>();
        }

        /// <summary>
        /// Completes the <see cref="DialogViewModel.Task"/> with the given result and raises the <see cref="Closed"/> event.
        /// </summary>
        protected void Close(TResult result)
        {
            _tcs.SetResult(result);

            Closed?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// A task promising the result of the dialog view model. It is completed when <see cref="Close"/> was called.
        /// </summary>
        public Task<TResult> Task => _tcs.Task;

        /// <summary>
        /// This event is raised when the dialog was closed.
        /// </summary>
        public event EventHandler Closed;
    }
}
