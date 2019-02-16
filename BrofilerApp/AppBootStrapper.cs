using System;
using System.Windows;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
          //  builder.RegisterType<DialogService>().As<IDialogService>();
        }
    }
}
