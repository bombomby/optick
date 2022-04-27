using System.Collections.Generic;
using Profiler.Controls;
using Profiler.InfrastructureMvvm;
using Profiler.ViewModels.Plots;

namespace Profiler.ViewModels
{
    public class PlotPanelsSettingsViewModel : BaseViewModel
    {
        public List<PlotsViewModel> CustomPanels { get; set; }
        
        public string CurrentPath
        {
            get => Settings.LocalSettings.Data.PlotPanelsSettingsFile;
            set
            {
                Settings.LocalSettings.Data.PlotPanelsSettingsFile = value;
                Settings.LocalSettings.Save();
                OnPropertyChanged();
            }
        }
    }
}