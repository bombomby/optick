using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using Profiler.InfrastructureMvvm;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Autofac;
using Profiler.Controls;

namespace Profiler.ViewModels
{
    public class PlatformSelectorViewModel : BaseViewModel
    {
        #region private fields

        Settings _config;

        #endregion

        #region properties

        private ObservableCollection<PlatformDescription> _platforms;
        public ObservableCollection<PlatformDescription>  Platforms
        {
            get { return _platforms; }
            set { SetField(ref _platforms, value); }
        }

        private PlatformDescription _activePlatform;
        public PlatformDescription ActivePlatform
        {
            get { return _activePlatform; }
            set { SetField(ref _activePlatform, value); }
        }

        private string _newIP;
        public string NewIP
        {
            get { return _newIP; }
            set { SetField(ref _newIP, value); }
        }

        private short _newPort;
        public short NewPort
        {
            get { return _newPort; }
            set { SetField(ref _newPort, value); }
        }

        #endregion

        #region commands

        private RelayCommandAsync _addConnectionCommand;
        public RelayCommandAsync AddConnectionCommand
        {
            get
            {
                return _addConnectionCommand ??
                    (_addConnectionCommand = new RelayCommandAsync(async () =>
                    {
                        IPAddress address = null;
                        IPAddress.TryParse(NewIP, out address);

                        await AddPlatformAsync(address, NewPort, true);                      
                    },
                  // Condition execute command
                  () => NewPort > 0 && IsIpValid(NewIP)
                  ));
            }
        }

        private RelayCommand _removeConnectionCommand;
        public RelayCommand RemoveConnectionCommand
        {
            get
            {
                return _removeConnectionCommand ??
                    (_removeConnectionCommand = new RelayCommand(obj =>
                    {
                        PlatformDescription platform = obj as PlatformDescription;
                        Platforms.Remove(platform);
                        ActivePlatform = Platforms.FirstOrDefault();
                    },
                  // Condition execute command
                  enable =>
                  {
                      PlatformDescription platform = enable as PlatformDescription;
                      return platform != null ? !platform.CoreType : false;
                  }
                  ));
            }
        }


        #endregion

        #region constructor

        public PlatformSelectorViewModel()
        {
            Platforms = new ObservableCollection<PlatformDescription>();

            PlatformDescription description = new PlatformDescription();

            using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
                _config = scope.Resolve<Settings>();

            // Set core connections
            foreach (var ip in Platform.GetPCAddresses().Distinct())
                _platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = ip, CoreType = true });

            _platforms.Add(new PlatformDescription() { Name = "Add Connection", IP = IPAddress.Loopback, Detailed = true, CoreType = true });

            // Get custom connections from Settings
            if (_config?.LocalSettings?.Data?.Connections != null)
                foreach (var item in _config.LocalSettings.Data.Connections)
                    Platforms.Add(new PlatformDescription() { Name = item.Name, IP = item.Address, Port = (short)item.Port, PlatformType = item.Target });
                                
            ActivePlatform = Platforms[0];

            NewPort = PlatformDescription.DEFAULT_PORT;

            Platforms.CollectionChanged += Platforms_CollectionChanged;
        }

        #endregion

        #region public methods

        #endregion


        #region private methods

        // Save collection to settings
        private void Platforms_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var customConnectios = Platforms.Where(x => x.Detailed == true);

            if (_config.LocalSettings.Data.Connections != null)
                _config.LocalSettings.Data.Connections.Clear();
            else
                _config.LocalSettings.Data.Connections = new List<Platform.Connection>();

            Task.Run(() =>
            {
                foreach (var item in customConnectios)
                {
                    _config.LocalSettings.Data.Connections.Add(new Platform.Connection() { Name = item.Name, Address = item.IP, Port = item.Port, Target = item.PlatformType });
                }

                _config.LocalSettings.Save();
            } );
        }


        private bool IsIpValid(string ip)
        {
            if (ip?.Length > 0)
            {
                IPAddress address = null;
                return IPAddress.TryParse(ip, out address);
            }
            else
                return false;
        }


        private async Task AddPlatformAsync(IPAddress ip, short port, bool autofocus)
        {
            if (ip.Equals(IPAddress.None) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Loopback))
                return;

            PingReply reply = await new Ping().SendPingAsync(ip, 16);

            if (reply.Status == IPStatus.Success)
            {
                string name = reply.Address.ToString();

                try
                {
                    IPHostEntry entry = await Dns.GetHostEntryAsync(reply.Address);
                    if (entry != null)
                        name = entry.HostName;
                }
                catch (SocketException) { }

                var newPlatform = new PlatformDescription() { Name = name, IP = reply.Address, Port = port };

                bool needAdd = true;

                foreach (PlatformDescription platform in Platforms)
                    if (platform.IP.Equals(reply.Address) && platform.Port == port)
                    {
                        newPlatform = platform;
                        needAdd = false;
                        break;
                    }
                

                if (needAdd)
                    Platforms.Add(newPlatform);

                if (autofocus)
                    ActivePlatform = newPlatform;
            }
        }

        #endregion

    }

    public class PlatformDescription : BaseViewModel
    {
        public  const short DEFAULT_PORT = 31313;

        public short Port { get; set; }
        public bool CoreType { get; set; }          // true for constant items
        public bool Detailed { get; set; }          // true for Add Connection
        public string Icon { get; set; }

        string _name;
        public string Name
        {
            get { return _name; }
            set {
                    SetField(ref _name, value);
                    if (PlatformType == Platform.Type.Unknown)
                        Icon = GetIconByComputerName(_name);
                }
        }

        IPAddress _ip = IPAddress.None;
        public IPAddress IP
        {
            get { return _ip; }
            set { SetField(ref _ip, value); }
        }

        Platform.Type _platformType;
        public Platform.Type PlatformType
        {
            get { return _platformType; }
            set {
                   SetField(ref _platformType, value);
                   Icon = GetIconByPlatformType(value);
                }
        }

        public string Status
        {
            get
            {
                return IP.Equals(IPAddress.None) ? "Disconnected" : IP.ToString();
            }
        }


        public PlatformDescription()
        {
            Port = DEFAULT_PORT;
            Detailed = false;
        }



        #region private methods

        private string GetIconByComputerName(string name)
        {
            string result = "appbar_os_windows_8";

            if (name.IndexOf("xbox", StringComparison.OrdinalIgnoreCase) != -1)
                result = "appbar_controller_xbox";
            else if (name.IndexOf("ps4", StringComparison.OrdinalIgnoreCase) != -1)
                result = "appbar_social_playstation";
            else if (name.IndexOf("linux", StringComparison.OrdinalIgnoreCase) != -1)
                result = "appbar_os_ubuntu";
            else if (name.IndexOf("mac", StringComparison.OrdinalIgnoreCase) != -1)
                result = "appbar_social_apple";

            if (name == "Add Connection")
                result = "appbar_network";

            return result;
        }

        private string GetIconByPlatformType(Platform.Type type)
        {
            switch (type)
            {
                case Platform.Type.Unknown:
                    return "appbar_network";
                case Platform.Type.Windows:
                    return "appbar_os_windows_8";
                case Platform.Type.Linux:
                    return "appbar_os_ubuntu";
                case Platform.Type.MacOS:
                    return "appbar_social_apple";
                case Platform.Type.XBox:
                    return "appbar_controller_xbox";
                case Platform.Type.Playstation:
                    return "appbar_social_playstation";
                default:
                    return "appbar_network";
            }
        }

        #endregion
    }
}
