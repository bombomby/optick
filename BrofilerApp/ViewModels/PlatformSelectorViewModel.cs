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

        ObservableCollection<PlatformDescription> _platforms;
        public ObservableCollection<PlatformDescription>  Platforms
        {
            get { return _platforms; }
            set { SetField(ref _platforms, value); }
        }

        PlatformDescription _activePlatform;
        public PlatformDescription ActivePlatform
        {
            get { return _activePlatform; }
            set { SetField(ref _activePlatform, value); }
        }

        #endregion

        #region constructor

        public PlatformSelectorViewModel()
        {
            Platforms = new ObservableCollection<PlatformDescription>();

          //  IPAddress ip = Platform.GetPCAddress();

       //     _platforms.Add(new PlatformDescription() { Name = Environment.MachineName, IP = ip, Icon = "appbar_os_windows_8" });

            //_platforms.Add(new PlatformDescription() { Name = "PS4", IP = Platform.GetPS4Address(), Icon = "appbar_social_playstation" });
            //_platforms.Add(new PlatformDescription() { Name = "Xbox", IP = Platform.GetXONEAddress(), Icon = "appbar_controller_xbox" });
            _platforms.Add(new PlatformDescription() { Name = "Network", IP = IPAddress.Loopback, Icon = "appbar_network", Detailed = true });

            ActivePlatform = Platforms[0];
        }

        #endregion

    }

    public class PlatformDescription : BaseViewModel
    {
        public const short DEFAULT_PORT = 31313;

        public string Name { get; set; }
        public short Port { get; set; }
        public string Icon { get; set; }
        public bool Detailed { get; set; }

        IPAddress _ip = IPAddress.None;
        public IPAddress IP
        {
            get { return _ip; }
            set { SetField(ref _ip, value); }
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
    }
}
