using Autofac;
using Profiler.InfrastructureMvvm;
using Profiler.ViewModels;
using Profiler.Views;

namespace Profiler
{
    public class AppBootStrapper: BootStrapperBase<MainViewModel>
    {
        protected override void ConfigureContainer(ContainerBuilder builder)
        {
            base.ConfigureContainer(builder);

            builder.RegisterType<MainViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<MainView>().SingleInstance();
            builder.RegisterType<SummaryViewerModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<SummaryViewer>().SingleInstance();
            builder.RegisterType<ScreenShotViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<ScreenShotView>();
            builder.RegisterType<FileDialogService>().As<IFileDialogService>();
        }
    }
}
