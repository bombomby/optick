using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Autofac;

namespace Profiler.InfrastructureMvvm
{
    public abstract class BootStrapperBase
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        protected BootStrapperBase()
        {
            Container = CreateContainer();

            Application.Current.Startup += OnStartup;
            Application.Current.Exit += OnExit;
        }

        private IContainer CreateContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<WindowManager>().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<DialogManager>().AsImplementedInterfaces().SingleInstance();

            ConfigureContainer(builder);

            var container = builder.Build();
            return container;
        }

        /// <summary>
        /// The Autofac container used by the application.
        /// </summary>
        public static IContainer Container { get; private set; }

        /// <summary>
        /// This methods allows the inherited class to register her/his classes.
        /// </summary>
        /// <param name="builder"></param>
        protected virtual void ConfigureContainer(ContainerBuilder builder) { }

        /// <summary>
        /// Called when the Run method of the Application object is called.
        /// </summary>
        /// <param name="sender">The sender of the <see cref="Application.Startup"/> event.</param>
        /// <param name="e">Contains the arguments of the Startup event.</param>
        protected virtual void OnStartup(object sender, StartupEventArgs e) { }

        /// <summary>
        /// Called just before an application shuts down, and cannot be canceled.
        /// </summary>
        /// <param name="sender">The sender of the <see cref="Application.Exit"/> event.</param>
        /// <param name="e">Contains the arguments of the Exit event.</param>
        protected virtual void OnExit(object sender, ExitEventArgs e)
        {
            Container.Dispose();
        }
    }

    /// <summary>
    /// Base class for bootstrapper.
    /// </summary>
    /// <typeparam name="TViewModel">The type of the main window's view model.</typeparam>
    public abstract class BootStrapperBase<TViewModel> : BootStrapperBase, IUiExecution
    {
        private Window _window;

        /// <inheritdoc />
        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);
            builder.RegisterInstance(this).As<IUiExecution>();
        }

        /// <inheritdoc />
        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            _window = Container.Resolve<IWindowManager>().ShowWindow<TViewModel>();
        }

        /// <inheritdoc />
        void IUiExecution.Execute(Action action)
        {
            var dispatcher = _window?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            dispatcher.Invoke(action);
        }

        /// <inheritdoc />
        public Task ExecuteAsync(Action action)
        {
            var dispatcher = _window?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            return dispatcher.InvokeAsync(action).Task;
        }
    }
}
