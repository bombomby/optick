using Autofac;
using Profiler.InfrastructureMvvm;
using Profiler.ViewModels;
using Profiler.Views;
using Profiler.Controls;

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
            builder.RegisterType<ScreenShotViewModel>().AsSelf().AsImplementedInterfaces();
            builder.RegisterType<ScreenShotView>().AsSelf();
            builder.RegisterType<FileDialogService>().As<IFileDialogService>();

			builder.RegisterType<TaskTrackerViewModel>().AsSelf().AsImplementedInterfaces();
			builder.RegisterType<TaskTrackerView>().AsSelf();
		}
    }
}
