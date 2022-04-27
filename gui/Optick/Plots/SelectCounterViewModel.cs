using System.Collections.ObjectModel;
using System.Windows.Media;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class SelectCounterViewModel : BaseViewModel
    {
        private ObservableCollection<AvailableCounterViewModel> _availableCounters = new ObservableCollection<AvailableCounterViewModel>();
        private string _selectedCounter;
        private Color _color;

        public SelectCounterViewModel()
        {
            UnitsViewModel = new UnitsViewModel();
        }
        
        public ObservableCollection<AvailableCounterViewModel> AvailableCounters
        {
            get => _availableCounters;
            set => SetProperty(ref _availableCounters, value);
        }

        public Color Color
        {
            get => _color;
            set => SetProperty(ref _color, value);
        }

        public string SelectedCounter
        {
            get => _selectedCounter;
            set => SetProperty(ref _selectedCounter, value);
        }
        
        public UnitsViewModel UnitsViewModel { get; }

        public void Clear()
        {
            _availableCounters.Clear();
            _selectedCounter = null;
        }
        
        public void TrySelectFirstCounter()
        {
            SelectedCounter = AvailableCounters.Count > 0 ? AvailableCounters[0].Key : null;
        }
        
        public void RemoveFromAvailabile(string counterKeyToRemove)
        {
            for (var index = 0; index < AvailableCounters.Count; index++)
            {
                if (AvailableCounters[index].Key == counterKeyToRemove)
                {
                    AvailableCounters.RemoveAt(index);
                    return;
                }
            }
        }
        
        public class AvailableCounterViewModel
        {
            public AvailableCounterViewModel(string key, string name)
            {
                Key = key;
                Name = name;
            }
            
            public string Key { get; }

            public string Name { get; }
        }
    }
}