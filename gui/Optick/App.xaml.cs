using System;
using System.Windows;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using Sentry;

namespace Profiler
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{        

        static App()
        {

		}

        protected override void OnStartup(StartupEventArgs e)
        {
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
			base.OnStartup(e);
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			ReportError(ex);
		}

		private void Optick_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			e.Handled = ReportError(e.Exception);
		}

		bool ReportError(Exception ex)
		{
			Exception rootException = ex;

			while (rootException.InnerException != null)
				rootException = rootException.InnerException;

			if (MessageBox.Show("Unhandled Exception:\n" + rootException.ToString(), "Optick Crashed! Send report?", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.OK)
			{
				using (SentrySdk.Init("https://52c8ab53c0cf47f28263fc211ebd4d38@sentry.io/1493349"))
				{
					SentrySdk.CaptureException(rootException);
				}
				return true;
			}
			return false;
		}
	}
}
