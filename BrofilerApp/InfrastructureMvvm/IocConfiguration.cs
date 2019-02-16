using Autofac;
using Profiler.Views;
using Profiler.ViewModels;
using Profiler.Services;

namespace Profiler.InfrastructureMvvm
{
    public class IocConfiguration : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //builder.RegisterType<WindowManager>().AsImplementedInterfaces().SingleInstance();
            ////builder.RegisterType<DialogManager>().AsImplementedInterfaces().SingleInstance();
            ////builder.RegisterType<FlyoutManager>().AsImplementedInterfaces().SingleInstance();

            //builder.RegisterType<MainViewModel>().AsSelf().AsImplementedInterfaces().SingleInstance();
            //builder.RegisterType<MainView>().SingleInstance();

            ////builder.RegisterType<SampleDialogView>().InstancePerDependency().AsSelf();
            ////builder.RegisterType<SampleDialogViewModel>().InstancePerDependency().AsSelf();

            ////builder.RegisterType<SampleFlyoutView>().InstancePerDependency().AsSelf();
            ////builder.RegisterType<SampleFlyoutViewModel>().InstancePerDependency().AsSelf();

            ////builder.RegisterType<SampleSubView>().InstancePerDependency().AsSelf();
            ////builder.RegisterType<SampleSubViewModel>().InstancePerDependency().AsSelf();

            //builder.RegisterType<DialogService>().As<IDialogService>();
        }
    }
}
