namespace OptickVSIX
{
    using EnvDTE;
    using EnvDTE80;
    using OptickVSIX.ViewModels;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
	using System.Windows;
	using System.Windows.Controls;

	/// <summary>
	/// Interaction logic for BuildProgressWindowControl.
	/// </summary>
	public partial class BuildProgressWindowControl : UserControl
	{
		BuildViewModel BuildVM { get; set; }

		BuildProgressState State { get; set; }

		Events Events { get; set; }
		BuildEvents BuildEvents { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildProgressWindowControl"/> class.
		/// </summary>
		public BuildProgressWindowControl(BuildProgressState state)
		{
			this.InitializeComponent();
			this.State = state;

			this.Events = state.DTE.Events;
			this.BuildEvents = state.DTE.Events.BuildEvents;

			this.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
			this.BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
		}

		private void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
		{
			BuildVM = new BuildViewModel();
			DataContext = BuildVM;

			BuildVM.Name = State.DTE.Solution.FullName;
			BuildVM.Start(Scope, Action);
		}

		private void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
		{
			BuildVM?.Finish(Scope, Action);
		}

		/// <summary>
		/// Handles click on the button by displaying a message box.
		/// </summary>
		/// <param name="sender">The event sender.</param>
		/// <param name="e">The event args.</param>
		[SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
		[SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]
		private void button1_Click(object sender, RoutedEventArgs e)
		{
			MessageBox.Show(
				string.Format(System.Globalization.CultureInfo.CurrentUICulture, "Invoked '{0}'", this.ToString()),
				"BuildProgressWindow");
		}
	}
}