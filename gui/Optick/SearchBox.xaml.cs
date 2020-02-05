using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Timers;

namespace Profiler
{

	/// <summary>
	/// Interaction logic for SearchBox.xaml
	/// </summary>
	public partial class SearchBox : UserControl
	{
		public SearchBox()
		{
			InitializeComponent();

			delayedTextUpdateTimer.Elapsed += new ElapsedEventHandler(OnDelayedTextUpdate);
			delayedTextUpdateTimer.AutoReset = false;
		}

		private void FilterText_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (!isFiltering)
				return;

			delayedTextUpdateTimer.Stop();
			delayedTextUpdateTimer.Start();
		}

		bool isFiltering = false;

		public bool IsFiltering { get { return isFiltering; } }

		public void SetFilterText(string text)
		{
			isFiltering = true;
			FilterText.Text = text;
			delayedTextUpdateTimer.Stop();
			delayedTextUpdateTimer.Start();
		}


		private void FilterText_GotFocus(object sender, RoutedEventArgs e)
		{
			if (!isFiltering)
			{
				FilterText.Text = String.Empty;
				isFiltering = true;
			}
		}

		private void FilterText_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter || e.SystemKey == Key.Enter)
			{
				TextEnter?.Invoke(FilterText.Text);
			}
		}

		Timer delayedTextUpdateTimer = new Timer(300);

		public String Text { get { return FilterText.Text; } }

		void OnDelayedTextUpdate(object sender, ElapsedEventArgs e)
		{
			Application.Current.Dispatcher.BeginInvoke(new Action(() => { DelayedTextChanged(FilterText.Text); }));
		}

		public delegate void DelayedTextChangedEventHandler(String text);
		public event DelayedTextChangedEventHandler DelayedTextChanged;

		public delegate void TextEnterEventHandler(String text);
		public event TextEnterEventHandler TextEnter;

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			FilterText.Text = String.Empty;
		}
	}
}
