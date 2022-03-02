using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Profiler.Data;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels
{
    public class SelectedCounterViewModel : BaseViewModel
    {
        public const double SelectedPointEllipseSize = 9;
        
        private Point _hoverPoint;
        private string _key;
        private string _name;
        private Brush _color;
        private ICommand _removeCommand;
        private PointCollection _points;

        public SelectedCounterViewModel(CounterModel model, string key, string name, Brush color, Action<SelectedCounterViewModel> onRemove,
            Point hoverPoint, Point hoverPointInScreenSpace, Units dataUnits, Units viewUnits)
        {
            Key = key;
            Name = name;
            Color = color;
            Model = model;
            _hoverPoint = hoverPoint;
            HoverPointInScreenSpace = hoverPointInScreenSpace;
            RemoveCommand = new RelayCommand<SelectedCounterViewModel>(onRemove);
            DataUnits = dataUnits;
            ViewUnits = viewUnits;
            UpdatePoints();
        }

        public CounterModel Model { get; set; }

        public string Key
        {
            get => _key;
            set => _key = value;
        }

        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public Brush Color
        {
            get => _color;
            set => _color = value;
        }

        public PointCollection Points
        {
            get => _points;
            set => SetProperty(ref _points, value);
        }
        
        public Point HoverPoint
        {
            get => _hoverPoint;
            set => SetProperty(ref _hoverPoint, value);
        }
        
        public Point HoverPointInScreenSpace
        {
            get => _hoverPoint;
            set => SetProperty(ref _hoverPoint, value);
        }

        public Units DataUnits { get; }

        public Units ViewUnits { get; }

        public double EllipseSize => SelectedPointEllipseSize;
        
        public ICommand RemoveCommand
        {
            get => _removeCommand;
            set => _removeCommand = value;
        }

        public void UpdatePoints()
        {
            if (Model == null)
            {
                Points = new PointCollection(Array.Empty<Point>());
                return;
            }

            var measurements = Model.Measurements;
            var points = measurements.Select(m =>
            {
                double simpleUnits = 0;
                switch (DataUnits)
                {
                    case Units.Byte:
                        simpleUnits = m.Value;
                        break;
                    case Units.KB:
                        simpleUnits = m.Value * 1024;
                        break;
                    case Units.MB:
                        simpleUnits = m.Value * 1024 * 1024;
                        break;
                    case Units.Nanosecond:
                        simpleUnits = m.Value;
                        break;
                    case Units.Millisecond:
                        simpleUnits = m.Value * 1000000;
                        break;
                    case Units.Second:
                        simpleUnits = m.Value * 1000000 * 1000;
                        break;
                }

                double convertedValue = 0;
                switch (ViewUnits)
                {
                    case Units.Byte:
                        convertedValue = simpleUnits;
                        break;
                    case Units.KB:
                        convertedValue = simpleUnits / 1024;
                        break;
                    case Units.MB:
                        convertedValue = simpleUnits / 1024 / 1024;
                        break;
                    case Units.Nanosecond:
                        convertedValue = simpleUnits;
                        break;
                    case Units.Millisecond:
                        convertedValue = simpleUnits / 1000000;
                        break;
                    case Units.Second:
                        convertedValue = simpleUnits / 1000000 / 1000;
                        break;
                    default:
                        // Do not convert
                        convertedValue = m.Value;
                        break;
                }

                return new Point(m.RelativeMSec, convertedValue);
            });
            Points = new PointCollection(points);
        }
    }
}