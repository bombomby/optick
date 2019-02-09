using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Diagnostics;
//using Autofac;
//using Profiler.Config;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
       // private IContainer _iocContainer;          

        static App()
        {
			AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(OnAssemblyResolve);
        }

		static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			return AutoEmbedLibs.EmbeddedAssembly.Get(args.Name);
		}

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Create IoC container
            //ContainerBuilder builder = new ContainerBuilder();
            //builder.RegisterModule<IocConfiguration>();
            //_iocContainer = builder.Build();

            //var mainWindow = new MainWindow();  //_iocContainer.Resolve(MainWindow);
            //mainWindow.DataContext = 
            //mainWindow.Show();
        }


    }
}
