using System;
using System.Collections.Generic;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class UnitsViewModel : BaseViewModel
    {
        private Units _selectedDataUnits;
        private Units _selectedViewUnits;
        private List<Units> _viewUnits;

        public UnitsViewModel()
        {
            SelectedDataUnits = Units.Byte;
        }
        
        private static readonly List<Units> SizeUnits = new List<Units>
        {
            Units.Byte,
            Units.KB,
            Units.MB
        };

        private static readonly List<Units> TimeUnits = new List<Units>
        {
            Units.Nanosecond,
            Units.Millisecond,
            Units.Second
        };
        
        public List<Units> DataUnits => new List<Units>
        {
            Units.Count,
            Units.Percent,
            Units.Byte,
            Units.KB,
            Units.MB,
            Units.Nanosecond,
            Units.Millisecond,
            Units.Second,
        };

        public Units SelectedDataUnits
        {
            get => _selectedDataUnits;
            set
            {
                var oldUnits = ViewUnits;
                switch (value)
                {
                    case Units.Byte:
                    case Units.KB:
                    case Units.MB:
                        ViewUnits = SizeUnits;
                        break;
                    case Units.Nanosecond:
                    case Units.Millisecond:
                    case Units.Second:
                        ViewUnits = TimeUnits;
                        break;
                    default:
                        ViewUnits = new List<Units>
                        {
                            value
                        };
                        break;
                }

                if (oldUnits != ViewUnits)
                {
                    SelectedViewUnits = ViewUnits[0];
                }
                
                SetProperty(ref _selectedDataUnits, value);
            }
        }

        public List<Units> ViewUnits
        {
            get => _viewUnits;
            set => SetProperty(ref _viewUnits, value);
        }

        public Units SelectedViewUnits
        {
            get => _selectedViewUnits;
            set => SetProperty(ref _selectedViewUnits, value);
        }
    }
}