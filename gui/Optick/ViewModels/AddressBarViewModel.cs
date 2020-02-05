using Profiler.Controls;
using Profiler.Data;
using Profiler.InfrastructureMvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Profiler.ViewModels
{
    class ConnectionVM : BaseViewModel
    {
        const UInt16 DEFAULT_PORT = 31318;

        private String _name;
        public String Name
        {
            get { return _name; }
            set { SetProperty(ref _name, value); }
        }

        private Platform.Type _target;
        public Platform.Type Target
        {
            get { return _target; }
            set { SetProperty(ref _target, value); OnPropertyChanged("Icon"); }
        }

        private String _address;
        public String Address
        {
            get { return _address; }
            set { SetProperty(ref _address, value); }
        }

        private SecureString _password;
        public SecureString Password
        {
            get { return _password; }
            set { SetProperty(ref _password, value); }
        }

        private UInt16 _port = DEFAULT_PORT;
        public UInt16 Port
        {
            get { return _port; }
            set { SetProperty(ref _port, value); }
        }

        private bool _canEdit;
        public bool CanEdit
        {
            get { return _canEdit; }
            set { SetProperty(ref _canEdit, value); }
        }

        private bool _canDelete;
		public bool CanDelete
        {
            get { return _canDelete; }
            set { SetProperty(ref _canDelete, value); }
        }

        public String Icon
        {
            get
            {
                switch (Target)
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
                    case Platform.Type.PS4:
                        return "appbar_social_playstation";
					case Platform.Type.Android:
						return "appbar_os_android";
					case Platform.Type.iOS:
						return "appbar_os_ios";
					default:
                        return "appbar_network";
                }
            }
        }

        public ConnectionVM() { }
        public ConnectionVM(Platform.Connection con)
        {
            Name = con.Name;
            Address = con.Address;
            Target = con.Target;
            Port = con.Port;
            Password = con.Password;
            CanDelete = true;
        }

        public Platform.Connection GetConnection()
        {
            return new Platform.Connection()
            {
                Name = this.Name,
                Address = this.Address,
                Target = this.Target,
                Port = this.Port,
                Password = this.Password,
            };
        }
    }

    class AddressBarViewModel : BaseViewModel
    {
        public ObservableCollection<ConnectionVM> Connections { get; set; } = new ObservableCollection<ConnectionVM>();

        private ConnectionVM _selection;
        public ConnectionVM Selection
        {
            get { return _selection; }
            set { SetProperty(ref _selection, value); }
        }

        public AddressBarViewModel()
        {
            Load();
        }

        public void Load()
        {
            List<IPAddress> addresses = Platform.GetPCAddresses();
            foreach (var ip in addresses)
                Connections.Add(new ConnectionVM() { Name = Environment.MachineName, Address = ip.ToString(), Target = Platform.Type.Windows, CanDelete = false });

            foreach (Platform.Connection con in Settings.LocalSettings.Data.Connections)
                Connections.Add(new ConnectionVM(con));

            AddEditableItem();

            Select(Settings.LocalSettings.Data.LastConnection);
        }

        void Select(Platform.Connection connection)
        {
			if (connection != null)
			{
				ConnectionVM item = Connections.FirstOrDefault(c => c.Address == connection.Address && c.Port == connection.Port);
				if (item != null)
				{
					Selection = item;
					return;
				}
			}
            Selection = Connections.FirstOrDefault();
        }

        void AddEditableItem()
        {
            if (Connections.FirstOrDefault(c => c.CanEdit == true) == null)
            {
                List<IPAddress> addresses = Platform.GetPCAddresses();
                Connections.Add(new ConnectionVM() { Name = "Network", Address = addresses.Count > 0 ? addresses[0].ToString() : "127.0.0.1", Target = Platform.Type.Unknown, CanDelete = false, CanEdit = true });
            }
        }

        public void Save()
        {
            var connectionList = Settings.LocalSettings.Data.Connections;
            connectionList.Clear();
            foreach (ConnectionVM con in Connections)
                if (con.CanDelete)
                    connectionList.Add(con.GetConnection());
            Settings.LocalSettings.Data.LastConnection = Selection.GetConnection();
            Settings.LocalSettings.Save();
        }

        public void Update(Platform.Connection connection)
        {
            ConnectionVM item = Connections.FirstOrDefault(c => c.Address == connection.Address && c.Port == connection.Port);
            if (item != null)
            {
                item.Name = String.IsNullOrEmpty(connection.Name) ? connection.Target.ToString() : connection.Name;
                item.Target = connection.Target;
                if (item.CanEdit)
                {
                    item.CanEdit = false;
                    item.CanDelete = true;
                }
            }
            else
            {
                item = new ConnectionVM(connection);
                Connections.Add(item);
            }
            AddEditableItem();
            Selection = item;
            Task.Run(()=>Save());
        }
    }
}
