using System;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// Declares methods to show dialogs.
    /// </summary>
    public interface IDialogManager
    {
        /// <summary>
        /// Shows a dialog asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="ViewLocator"/> to get the view matching the given view model. That view must
        /// derive from <see cref="BaseMetroDialog"/>.
        /// </para>
        /// <para>
        /// The <a href="http://mahapps.com/controls/buttons.html">MahApps.Metro style "FlatButton"</a> will be added
        /// to the view's resources.
        /// </para>
        /// </remarks>
        /// <param name="viewModel">The view model for the view to be displayed.</param>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <returns>A <see cref="Task"/> object which is completed when the dialog is closed.</returns>
        Task ShowDialogAsync(DialogViewModel viewModel, MetroDialogSettings settings = null);

        /// <summary>
        /// Shows a dialog asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="ViewLocator"/> to get the view matching the given view model. That view must
        /// derive from <see cref="BaseMetroDialog"/>.
        /// </para>
        /// <para>
        /// The <a href="http://mahapps.com/controls/buttons.html">MahApps.Metro style "FlatButton"</a> will be added
        /// to the view's resources.
        /// </para>
        /// </remarks>
        /// <typeparam name="TViewModel">The type of the view model.</typeparam>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <returns>A <see cref="Task"/> object which is completed when the dialog is closed.</returns>
        Task ShowDialogAsync<TViewModel>(MetroDialogSettings settings = null) where TViewModel : DialogViewModel;

        /// <summary>
        /// Shows a dialog asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="ViewLocator"/> to get the view matching the given view model. That view must
        /// derive from <see cref="BaseMetroDialog"/>.
        /// </para>
        /// <para>
        /// The <a href="http://mahapps.com/controls/buttons.html">MahApps.Metro style "FlatButton"</a> will be added
        /// to the view's resources.
        /// </para>
        /// </remarks>
        /// <param name="viewModel">The view model for the view to be displayed.</param>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <typeparam name="TResult">The type of the view model's result.</typeparam>
        /// <returns>The result of the view model.</returns>
        Task<TResult> ShowDialogAsync<TResult>(DialogViewModel<TResult> viewModel, MetroDialogSettings settings = null);

        /// <summary>
        /// Shows a dialog asynchronously.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method uses <see cref="ViewLocator"/> to get the view matching the given view model. That view must
        /// derive from <see cref="BaseMetroDialog"/>.
        /// </para>
        /// <para>
        /// The <a href="http://mahapps.com/controls/buttons.html">MahApps.Metro style "FlatButton"</a> will be added
        /// to the view's resources.
        /// </para>
        /// </remarks>
        /// <typeparam name="TViewModel">The type of the view model.</typeparam>
        /// <typeparam name="TResult">The type of the view model's result.</typeparam>
        /// <param name="settings">An optional pre-defined settings instance.</param>
        /// <returns>The result of the view model.</returns>
        Task<TResult> ShowDialogAsync<TViewModel, TResult>(MetroDialogSettings settings = null) where TViewModel : DialogViewModel<TResult>;

        /// <summary>
        /// Displays a message box asynchronously.
        /// </summary>
        /// <param name="title">The title of the message box.</param>
        /// <param name="message">The content of the message box.</param>
        /// <param name="style">The style of the message box.</param>
        /// <param name="settings">Option settings for the message box.</param>
        /// <returns>A task promising the result of which button was pressed.</returns>
        Task<MessageDialogResult> ShowMessageBox(string title, string message, MessageDialogStyle style = MessageDialogStyle.Affirmative,
            MetroDialogSettings settings = null);
    }
}
