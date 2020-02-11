namespace OptickVSIX
{
	using System;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell;

	/// <summary>
	/// This class implements the tool window exposed by this package and hosts a user control.
	/// </summary>
	/// <remarks>
	/// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
	/// usually implemented by the package implementer.
	/// <para>
	/// This class derives from the ToolWindowPane class provided from the MPF in order to use its
	/// implementation of the IVsUIElementPane interface.
	/// </para>
	/// </remarks>
	[Guid(WindowGuidString)]
	public class BuildProgressWindow : ToolWindowPane
	{
		public const string WindowGuidString = "86d4c770-4241-4cb2-a4b2-c79ac8b95834";
		public const string Title = "Build Progress Window";

		/// <summary>
		/// Initializes a new instance of the <see cref="BuildProgressWindow"/> class.
		/// </summary>
		public BuildProgressWindow(BuildProgressState state) : base(null)
		{
			this.Caption = Title;

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.
			this.Content = new BuildProgressWindowControl(state);
		}
	}
}
