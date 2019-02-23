using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.ObjectModel;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class PlatformSelectorViewModel : BaseViewModel
    {
        #region private fields



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

        private string _newPort;
        public string NewPort
        {
            get { return _newPort; }
            set { SetField(ref _newPort, value); }
        }

        #endregion

        #region commands

        private RelayCommandAsync _addConnection;
        public RelayCommandAsync AddConnection
        {
            get
            {
                return _addConnection ??
                    (_addConnection = new RelayCommandAsync(async() => 
                    {
                       // await Task.Run();
                    },
                  // Condition execute command
                  () => NewPort != null && NewIP != null
                  ));
            }
        }


        #endregion

        #region constructor

        public PlatformSelectorViewModel()
        {
            Platforms = new ObservableCollection<PlatformDescription>();

            PlatformDescription description = new PlatformDescription();

            foreach (var ip in Platform.GetPCAddresses().Distinct())
                _platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = ip, CoreType = true });

            _platforms.Add(new PlatformDescription() { Name = "Add Connection", IP = IPAddress.Loopback, Detailed = true, CoreType = true });

            ActivePlatform = Platforms[0];


        }

        #endregion

        #region public methods

        #endregion


        #region private methods



        #endregion

       


    }

    public class PlatformDescription : BaseViewModel
    {
        private const short DEFAULT_PORT = 31313;

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
                   Icon = GetIconByPlatformType(_platformType);
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
