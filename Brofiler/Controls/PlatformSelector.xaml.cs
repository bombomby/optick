using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

using System.Net;
using System.ComponentModel;

namespace Profiler
{
    /// <summary>
    /// Interaction logic for PlatformSelector.xaml
    /// </summary>
    public partial class PlatformSelector : UserControl
    {
        public class PlatformDescription : INotifyPropertyChanged
        {
            public String Name { get; set; }

            IPAddress ip = IPAddress.None;
            public IPAddress IP
            {
                get
                {
                    return ip;
                }
                set
                {
                    ip = value;
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("Status"));
                }
            }  

            public short Port { get; set; }

            public String Icon { get; set; }

            public bool Detailed { get; set; }

            public String Status
            {
                get
                {
                    return IP.Equals(IPAddress.None) ? "Disconnected" : IP.ToString();
                }
            }

            const short DEFAULT_PORT = 31313;

            public event PropertyChangedEventHandler PropertyChanged;

            public PlatformDescription()
            {
                Port = DEFAULT_PORT;
            }
        }

        public PlatformDescription ActivePlatform
        {
            get
            {
                return comboBox.SelectedItem as PlatformDescription;
            }
        }

        List<PlatformDescription> platforms = new List<PlatformDescription>();

        public PlatformSelector()
        {
            InitializeComponent();

            platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = Platform.GetPCAddress(), Icon = "appbar_os_windows_8" });
            platforms.Add(new PlatformDescription() { Name = "PS4", IP = Platform.GetPS4Address(),  Icon = "appbar_social_playstation" });
            platforms.Add(new PlatformDescription() { Name = "Xbox", IP = Platform.GetXONEAddress(), Icon = "appbar_controller_xbox" });
            platforms.Add(new PlatformDescription() { Name = "Network", IP = IPAddress.Loopback, Icon = "appbar_network", Detailed = true });

            comboBox.ItemsSource = platforms;

        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlatformDescription desc = comboBox.SelectedItem as PlatformDescription;

            if (desc !=null)
            {
                IP.Text = desc.IP.ToString();
                Port.Text = desc.Port.ToString();
                DetailedIP.Visibility = desc != null && desc.Detailed ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void IP_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlatformDescription desc = comboBox.SelectedItem as PlatformDescription;
            if (desc != null)
            {
                IPAddress address = null;
                if (IPAddress.TryParse(IP.Text, out address))
                    desc.IP = address;
            }
        }

        private void Port_TextChanged(object sender, TextChangedEventArgs e)
        {
            PlatformDescription desc = comboBox.SelectedItem as PlatformDescription;
            if (desc != null)
            {
                short port = 0;
                if (short.TryParse(Port.Text, out port))
                    desc.Port = port;
            }
        }

        
    }
}
