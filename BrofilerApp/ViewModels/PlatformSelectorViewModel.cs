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
using System.Windows;

namespace Profiler.ViewModels
{
    public class PlatformSelectorViewModel : BaseViewModel
    {
        #region private fields

        Settings _config;

        bool _isSelfUpdateFlag = false;     // is true if current application update the LocalSettings 

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
            set {
                    if(value !=null)
                        SetField(ref _activePlatform, value);
                    else
                        SetField(ref _activePlatform, _activePlatform = Platforms[0]);

                if (!ActivePlatform.CoreType)
                 //   Application.Current.Dispatcher.BeginInvoke(new Action(async() => ActivePlatform.IsDisconnect = !(await CheckHostEnable(ActivePlatform.IP))));
                  Task.Run(async()=> ActivePlatform.IsDisconnect = !( await CheckHostEnable(ActivePlatform.IP)) );

                //NewIP = _activePlatform.IP.ToString();
                //NewPort = _activePlatform.Port;
            }
        }

        //private string _newIP;
        //public string NewIP
        //{
        //    get { return _newIP; }
        //    set
        //    {
        //        IPAddress address = null;
        //        IPAddress.TryParse(value, out address);
        //        ActivePlatform.IP = address;

        //        SetField(ref _newIP, value);
        //    }
        //}

        //private short _newPort;
        //public short NewPort
        //{
        //    get { return _newPort; }
        //    set
        //    {
        //        ActivePlatform.Port = value;
        //        SetField(ref _newPort, value);
        //    }
        //}

        #endregion

        #region commands

        //private RelayCommandAsync _addConnectionCommand;
        //public RelayCommandAsync AddConnectionCommand
        //{
        //    get
        //    {
        //        return _addConnectionCommand ??
        //            (_addConnectionCommand = new RelayCommandAsync(async () =>
        //            {
        //                IPAddress address = null;
        //                IPAddress.TryParse(NewIP, out address);

        //                await AddPlatformAsync(address, NewPort, true);                      
        //            },
        //          // Condition execute command
        //          () => NewPort > 0 && IsIpValid(NewIP)
        //          ));
        //    }
        //}

        private RelayCommandGenericAsync<object> _removeConnectionCommand;
        public RelayCommandGenericAsync<object> RemoveConnectionCommand
        {
            get
            {
                return _removeConnectionCommand ??
                    (_removeConnectionCommand = new RelayCommandGenericAsync<object>(async obj =>
                    {
                        PlatformDescription platform = obj as PlatformDescription;

                        if(platform == ActivePlatform)
                            ActivePlatform = Platforms.FirstOrDefault();

                        Platforms.Remove(platform);
                        await RemoveCustomConnectionFromLocalSettingsAsync(platform);
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

            using (var scope = BootStrapperBase.Container.BeginLifetimeScope())
                _config = scope.Resolve<Settings>();

            // Set core connections
            foreach (var ip in Platform.GetPCAddresses())
                _platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = ip, CoreType = true, PlatformType = Platform.Type.Windows });

            _platforms.Add(new PlatformDescription() { Name = "Network", IP = IPAddress.Loopback, Port = PlatformDescription.DEFAULT_PORT, Detailed = true, CoreType = true });

            SetCustomPlatformsFromLocalSettings();

            _config.LocalSettings.OnChanged += LocalSettings_OnChanged;
        }

        #endregion

        #region public methods

        // Update type platform
        public void PlatformUpdate(PlatformDescription platform)
        {
            // Restore Network platform
            Platforms[1].IP = IPAddress.Loopback;
            Platforms[1].Port = PlatformDescription.DEFAULT_PORT;

            try
            {
                // Add new platform
                Task.Run(() => AddPlatformAsync(platform));
                //  Task.Run(() => AddCustomConnectionToLocalSettingsAsync(platform));
            }
            catch (Exception e)
            {
            }

        }

        #endregion


        #region private methods

        // Get custom connections from LocalSettings
        private void SetCustomPlatformsFromLocalSettings()
        {
            PlatformDescription tempActivePlatform = ActivePlatform;
            ActivePlatform = Platforms.FirstOrDefault();

            foreach (var item in Platforms.ToList())
                if (item.CoreType == false)
                    Platforms.Remove(item);

            // Get custom connections from Settings
            if (_config?.LocalSettings?.Data?.Connections != null)
                foreach (var item in _config.LocalSettings.Data.Connections)
                    Platforms.Add(new PlatformDescription() { Name = item.Name, IP = item.Address, Port = (short)item.Port, PlatformType = item.Target });

            if (Platforms.Contains(tempActivePlatform))
                ActivePlatform = tempActivePlatform;

        }

        // Add new custom connection to LocalSettings
        private async Task AddCustomConnectionToLocalSettingsAsync(PlatformDescription platform)
        {
            _isSelfUpdateFlag = true;

            await Task.Run(() =>
            {
                try
                {
                    _config.LocalSettings.Data.Connections.Add(new Platform.Connection()
                    { Name = platform.Name, Address = platform.IP, Port = platform.Port, Target = platform.PlatformType });

                    _config.LocalSettings.Save();
                }
                catch (Exception)
                {
                }
            });
        }

        // Remove custom connection from LocalSettings
        private async Task RemoveCustomConnectionFromLocalSettingsAsync(PlatformDescription platform)
        {
            _isSelfUpdateFlag = true;

            await Task.Run(() =>
            {
                try
                {
                   var connection = _config.LocalSettings.Data.Connections.FirstOrDefault(x => x.Address == platform.IP && x.Port == platform.Port);
                    // in case many application get access to LocalSettings and is selected union platform
                    // connection cant determine by IP and Port
                    if (connection==null)
                        connection = _config.LocalSettings.Data.Connections.FirstOrDefault(x => x.Name == platform.Name);

                    bool count = _config.LocalSettings.Data.Connections.Remove(connection);

                    if (count)
                        _config.LocalSettings.Save();
                }
                catch (Exception)
                {
                }

            });
        }

        // Update custom connection in LocalSettings
        private async Task UpdateCustomConnectionToLocalSettingsAsync(PlatformDescription platform)
        {
            _isSelfUpdateFlag = true;

            await Task.Run(() =>
            {
                try
                {
                    var connection = _config.LocalSettings.Data.Connections.FirstOrDefault(x => x.Address == platform.IP && x.Port == platform.Port);

                    if (connection == null)
                        connection = _config.LocalSettings.Data.Connections.FirstOrDefault(x => x.Name == platform.Name);

                    bool count = _config.LocalSettings.Data.Connections.Remove(connection);

                    if (count)
                        _config.LocalSettings.Data.Connections.Add(new Platform.Connection()
                        { Name = platform.Name, Address = platform.IP, Port = platform.Port, Target = platform.PlatformType });

                    _config.LocalSettings.Save();
                }
                catch (Exception)
                {
                }

            });
        }

        // Save all custom collection to LocalSettings
        private async Task SaveAllCustomConnectionsToLocalSettingsAsync()
        {
            var customConnectios = Platforms.Where(x => x.CoreType == false);

            _isSelfUpdateFlag = true;

            await Task.Run(() =>
            {
                try
                {
                    if (_config.LocalSettings.Data.Connections != null)
                        _config.LocalSettings.Data.Connections.Clear();
                    else
                        _config.LocalSettings.Data.Connections = new List<Platform.Connection>();

                    foreach (var item in customConnectios)
                        _config.LocalSettings.Data.Connections.Add(new Platform.Connection() { Name = item.Name, Address = item.IP, Port = item.Port, Target = item.PlatformType });

                    _config.LocalSettings.Save();
                }
                catch (Exception)
                {
                }             
            } );
        }

        // Update collection platforms if LocalSettings changed by other application
        private void LocalSettings_OnChanged()
        {
            if  (!_isSelfUpdateFlag)
                 Application.Current.Dispatcher.BeginInvoke(new Action(() =>SetCustomPlatformsFromLocalSettings() ));

            _isSelfUpdateFlag = false;
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


        private async Task AddPlatformAsync(PlatformDescription platform)
        {
            if (platform.IP.Equals(IPAddress.None) || platform.IP.Equals(IPAddress.Any) || platform.IP.Equals(IPAddress.Loopback))
                return;

            try
            {
                if (Platforms.Where(x => x.IP.Equals(platform.IP) && x.Port == platform.Port).Count() == 0)
                {
                    await Application.Current.Dispatcher.BeginInvoke(new Action(()=>Platforms.Add(platform)));
                    await AddCustomConnectionToLocalSettingsAsync(platform);
                }
                ActivePlatform = Platforms.FirstOrDefault(x => x.IP.Equals(platform.IP) && x.Port == platform.Port);
            }
            catch (Exception e)
            {
                throw e;
            }

        }

        private async Task<bool> CheckHostEnable(IPAddress ip)
        {
            if (ip.Equals(IPAddress.None) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Loopback))
               return false;

            PingReply reply = await new Ping().SendPingAsync(ip, 1000);

            return reply.Status == IPStatus.Success ? true : false;
        }

        //private async Task AddPlatformAsync(IPAddress ip, short port, bool autofocus)
        //{
        //    if (ip.Equals(IPAddress.None) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Loopback))
        //        return;

        //    PingReply reply = await new Ping().SendPingAsync(ip, 1000);

        //    if (reply.Status == IPStatus.Success)
        //    {
        //        string name = reply.Address.ToString();

        //        try
        //        {
        //            IPHostEntry entry = await Dns.GetHostEntryAsync(reply.Address);
        //            if (entry != null)
        //                name = entry.HostName;
        //        }
        //        catch (SocketException) { }

        //        var newPlatform = new PlatformDescription() { Name = name, IP = reply.Address, Port = port };

        //        bool needAdd = true;

        //        foreach (PlatformDescription platform in Platforms)
        //            if (platform.IP.Equals(reply.Address) && platform.Port == port)
        //            {
        //                newPlatform = platform;
        //                needAdd = false;
        //                break;
        //            }


        //        if (needAdd)
        //        {
        //            Platforms.Add(newPlatform);
        //            await AddCustomConnectionToLocalSettingsAsync(newPlatform);
        //        }

        //        if (autofocus)
        //            ActivePlatform = newPlatform;
        //    }
        //}

        #endregion

    }

    public class PlatformDescription : BaseViewModel
    {
        public  const short DEFAULT_PORT = 31313;

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
            set
            {
                SetField(ref _ip, value);
                OnPropertyChanged("Status");
            }
        }


        private short _port;
        public short Port
        {
            get { return _port; }
            set
            {
                SetField(ref _port, value);
                OnPropertyChanged("Status");
            }
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

        private bool _isDisconnect;
        public bool IsDisconnect
        {
            get { return _isDisconnect; }
            set { SetField(ref _isDisconnect, value);}
        }

        public string Status
        {
            get
            {
                return IP.Equals(IPAddress.None) ? "Disconnected" : String.Format("{0} : {1}",IP.ToString(),Port);
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
            string result = "appbar_network";

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
