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
    /// Interaction logic for AddressBarView.xaml
    /// </summary>
    public partial class AddressBarView : UserControl
    {
        public AddressBarView()
        {
            InitializeComponent();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AddressBarViewModel)
            {
                ConnectionVM con = (DataContext as AddressBarViewModel).Selection;
                if (con != null)
                {
                    con.Password = PwdBox.SecurePassword;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConnectionVM con = ConnectionComboBox.SelectedItem as ConnectionVM;
            if (con != null && con.CanEdit)
                PwdBox.Password = null;
        }

		private void MenuItem_Remove(object sender, RoutedEventArgs e)
		{
			ConnectionVM connection = (e.Source as FrameworkElement).DataContext as ConnectionVM;
			if (connection.CanDelete)
				(DataContext as AddressBarViewModel).Connections.Remove(connection);
		}
	}
}
