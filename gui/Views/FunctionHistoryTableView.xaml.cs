using Profiler.Data;
using Profiler.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Profiler.Views
{
	/// <summary>
	/// Interaction logic for FunctionHistoryTableView.xaml
	/// </summary>
	public partial class FunctionHistoryTableView : UserControl
    {
        public FunctionHistoryTableView()
        {
            InitializeComponent();
        }

		private void FunctionInstanceDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			List<int> indices = new List<int>();

			foreach (FunctionStats.Sample sample in FunctionInstanceDataGrid.SelectedItems)
				indices.Add(sample.Index);

			if (indices.Count > 0)
			{
				FunctionViewModel vm = DataContext as FunctionViewModel;
				if (vm != null)
					vm.OnDataClick(this, indices);
			}
		}
	}
}
