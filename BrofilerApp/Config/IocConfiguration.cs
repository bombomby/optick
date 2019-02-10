using Autofac;
using Profiler.Controls.View;
using Profiler.Controls.ViewModel;
using Profiler.Services;

namespace Profiler.Config
{
    public class IocConfiguration : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // optional: chain ServiceModule with other modules for going deeper down in the architecture: 
           // builder.RegisterModule<MainWindow>();

            builder.RegisterType<DialogService>().As<IDialogService>();
        }
    }
}
