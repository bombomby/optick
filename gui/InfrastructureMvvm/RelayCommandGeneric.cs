using System;
using System.Windows.Input;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// The <see cref="RelayCommand{T}"/> class assigns an <see cref="Action{T}"/> delegate to the <see cref="Execute"/>
    /// method and a <see cref="Func{T, T}"/> delegate to the <see cref="CanExecute"/> method,
    /// allowing <see cref="ICommand"/> objects to be implemented completely from within a View-Model.
    /// </summary>
    /// <typeparam name="T">
    /// Determines the parameter type that will be passed in to your
    /// <see cref="Action{T}"/> and <see cref="Func{T, T}"/> delegates.
    /// </typeparam>
    public class RelayCommand<T> : ICommand
    {
        #region Private Fields

        private readonly Func<T, bool> _canExecute;
        private readonly Action<T> _execute;

        #endregion Private Fields

        #region Constructors

        /// <summary>
        /// Create a new <see cref="RelayCommand"/> with the given execution delegate.
        /// </summary>
        /// <param name="execute">The <see cref="Action"/> to execute when the
        /// <see cref="Execute"/> method is called.</param>
        public RelayCommand(Action<T> execute)
            : this(execute, null)
        { }

        /// <summary>
        /// Create a new <see cref="RelayCommand"/> with the given execution delegate.
        /// </summary>
        /// <param name="execute">The <see cref="Action"/> to execute when the
        /// <see cref="Execute"/> method is called.</param>
        /// <param name="canExecute">The <see cref="Func{T, T}"/> to execute
        /// when the <see cref="CanExecute"/> method is queried.</param>
        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            if (execute == null)
                throw new ArgumentNullException("execute");

            _execute = execute;
            _canExecute = canExecute;
        }

        #endregion Constructors

        #region ICommand Implementation

        /// <summary>
        /// Event which is raised when the command's ability to be executed changes.
        /// </summary>
        public event EventHandler CanExecuteChanged = null;

        /// <summary>
        /// Determine if the command can be executed in its current state.
        /// </summary>
        /// <param name="parameter">An optional parameter.</param>
        /// <returns>Returns True if the command can be executed.  Otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            var canExecuteHandler = _canExecute;
            if (canExecuteHandler != null)
            {
                return canExecuteHandler((T)parameter);
            }

            return true;
        }

        /// <summary>
        /// Execute the command's delegate method.
        /// </summary>
        /// <param name="parameter">An optional parameter.</param>
        public void Execute(object parameter)
        {
            var executeHandler = _execute;
            if (executeHandler != null)
            {
                executeHandler((T)parameter);
            }
        }

        #endregion ICommand Implementation


        #region IRaiseCanExecuteChanged Implementation

        /// <summary>
        /// Raise the <see cref="ICommand.CanExecuteChanged"/> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        #endregion IRaiseCanExecuteChanged Implementation
    }
}

