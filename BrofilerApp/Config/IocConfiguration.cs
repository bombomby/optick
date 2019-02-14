using Autofac;
using Profiler.View;
using Profiler.ViewModel;
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
