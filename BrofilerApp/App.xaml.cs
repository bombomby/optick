using System;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{        

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
        }


    }
}
