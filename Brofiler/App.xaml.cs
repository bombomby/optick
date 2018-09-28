using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
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

	}
}
