using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using Profiler.InfrastructureMvvm;
using System.Net.NetworkInformation;
using Autofac;
using Profiler.Controls;
using System.Windows;

namespace Profiler.ViewModels
{
    public class PlatformSelectorViewModel : BaseViewModel
    {
        #region private fields

        bool _isSelfUpdateFlag = false;     // is true if current application update the LocalSettings 

        #endregion

        #region properties

        private ObservableCollection<PlatformDescription> _platforms;
        public ObservableCollection<PlatformDescription>  Platforms
        {
            get { return _platforms; }
            set { SetProperty(ref _platforms, value); }
        }

        private PlatformDescription _activePlatform;
        public PlatformDescription ActivePlatform
        {
            get { return _activePlatform; }
            set {
                    if(value !=null)
                        SetProperty(ref _activePlatform, value);
                    else
                        SetProperty(ref _activePlatform, _activePlatform = Platforms[0]);

                if (!ActivePlatform.CoreType)
                  Task.Run(async()=> ActivePlatform.IsDisconnect = !( await CheckHostEnable(ActivePlatform.IP)) );
            }
        }

        #endregion

        #region commands

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

            // Set core connections
            foreach (var ip in Platform.GetPCAddresses())
                _platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = ip, CoreType = true, PlatformType = Platform.Type.Windows });

            _platforms.Add(new PlatformDescription() { Name = "Network", IP = IPAddress.Loopback, Port = PlatformDescription.DEFAULT_PORT, Detailed = true, CoreType = true });

            SetCustomPlatformsFromLocalSettings();

            Settings.LocalSettings.OnChanged += LocalSettings_OnChanged;
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
            }
            catch (Exception)
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
            if (Settings.LocalSettings.Data.Connections != null)
                foreach (var item in Settings.LocalSettings.Data.Connections)
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
					Settings.LocalSettings.Data.Connections.Add(new Platform.Connection()
                    { Name = platform.Name, Address = platform.IP, Port = platform.Port, Target = platform.PlatformType });

					Settings.LocalSettings.Save();
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
                   var connection = Settings.LocalSettings.Data.Connections.FirstOrDefault(x => x.Address == platform.IP && x.Port == platform.Port);
                    // in case many application get access to LocalSettings and is selected union platform
                    // connection cant determine by IP and Port
                    if (connection==null)
                        connection = Settings.LocalSettings.Data.Connections.FirstOrDefault(x => x.Name == platform.Name);

                    bool count = Settings.LocalSettings.Data.Connections.Remove(connection);

                    if (count)
						Settings.LocalSettings.Save();
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
                    var connection = Settings.LocalSettings.Data.Connections.FirstOrDefault(x => x.Address == platform.IP && x.Port == platform.Port);

                    if (connection == null)
                        connection = Settings.LocalSettings.Data.Connections.FirstOrDefault(x => x.Name == platform.Name);

                    bool count = Settings.LocalSettings.Data.Connections.Remove(connection);

                    if (count)
						Settings.LocalSettings.Data.Connections.Add(new Platform.Connection()
                        { Name = platform.Name, Address = platform.IP, Port = platform.Port, Target = platform.PlatformType });

					Settings.LocalSettings.Save();
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
                    if (Settings.LocalSettings.Data.Connections != null)
						Settings.LocalSettings.Data.Connections.Clear();
                    else
						Settings.LocalSettings.Data.Connections = new List<Platform.Connection>();

                    foreach (var item in customConnectios)
						Settings.LocalSettings.Data.Connections.Add(new Platform.Connection() { Name = item.Name, Address = item.IP, Port = item.Port, Target = item.PlatformType });

					Settings.LocalSettings.Save();
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
            catch (Exception)
            {
            }
        }

        private async Task<bool> CheckHostEnable(IPAddress ip)
        {
            if (ip.Equals(IPAddress.None) || ip.Equals(IPAddress.Any) || ip.Equals(IPAddress.Loopback))
               return false;

            PingReply reply = await new Ping().SendPingAsync(ip, 1000);

            return reply.Status == IPStatus.Success ? true : false;
        }

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
                    SetProperty(ref _name, value);
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
                SetProperty(ref _ip, value);
                OnPropertyChanged("Status");
            }
        }

        private short _port;
        public short Port
        {
            get { return _port; }
            set
            {
                SetProperty(ref _port, value);
                OnPropertyChanged("Status");
            }
        }

        Platform.Type _platformType;
        public Platform.Type PlatformType
        {
            get { return _platformType; }
            set {
                   SetProperty(ref _platformType, value);
                   Icon = GetIconByPlatformType(value);
                }
        }

        private bool _isDisconnect;
        public bool IsDisconnect
        {
            get { return _isDisconnect; }
            set { SetProperty(ref _isDisconnect, value);}
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
