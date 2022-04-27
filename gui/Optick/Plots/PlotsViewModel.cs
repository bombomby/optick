using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using Profiler.Data;
using Profiler.InfrastructureMvvm;

namespace Profiler.ViewModels.Plots
{
    public class PlotsViewModel : BaseViewModel
    {
        private readonly Random _rnd = new Random();

        private Dictionary<string, CounterModel> _model;
        private SelectCounterViewModel _selectCounterViewModel;
        private ObservableCollection<SelectedCounterViewModel> _selectedCounterViewModels;
        private double _mousePositionX;
        private double _mousePositionY;
        private Visibility _hoverVisibility;
        private double _yMin;
        private double _yMax;
        private double _height;
        private string _title;
        private bool _isAutoFitEnabled;
        private double _xMin;
        private double _width;


        public PlotsViewModel(string title, Dictionary<string, CounterModel> model)
        {
            Title = title;
            _selectCounterViewModel = new SelectCounterViewModel
            {
                Color = Color.FromRgb((byte)_rnd.Next(0, 255), (byte)_rnd.Next(0, 255), (byte)_rnd.Next(0, 255))
            };
            _selectedCounterViewModels = new ObservableCollection<SelectedCounterViewModel>();

            SetModel(model);
        }

        public HashSet<string> SelectedCounterKeys { get; } = new HashSet<string>();

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public SelectCounterViewModel SelectCounterViewModel
        {
            get => _selectCounterViewModel;
            set => SetProperty(ref _selectCounterViewModel, value);
        }

        public double MousePositionX
        {
            get => _mousePositionX;
            set => SetProperty(ref _mousePositionX, value);
        }

        public double MousePositionY
        {
            get => _mousePositionY;
            set => SetProperty(ref _mousePositionY, value);
        }

        public ObservableCollection<SelectedCounterViewModel> SelectedCounterViewModels
        {
            get => _selectedCounterViewModels;
            set => SetProperty(ref _selectedCounterViewModels, value);
        }

        public Visibility HoverVisibility
        {
            get => _hoverVisibility;
            set => SetProperty(ref _hoverVisibility, value);
        }

        public double YMin
        {
            get => _yMin;
            set => SetAxis(nameof(YMin), false, ref _yMin, value);
        }

        public double YMax
        {
            get => _yMax;
            set => SetAxis(nameof(YMax), false, ref _yMax, value);
        }

        public double Height
        {
            get => _height;
            set
            {
                SetProperty(ref _height, value);
                SetAxis(nameof(YMax), true, ref _yMax, _yMin + value);
            }
        }

        public bool IsAutoFitEnabled
        {
            get => _isAutoFitEnabled;
            set => SetProperty(ref _isAutoFitEnabled, value);
        }

        public double XMin
        {
            get => _xMin;
            set => SetProperty(ref _xMin, value);
        }
        
        public double Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
        
        public void SetModel(Dictionary<string, CounterModel> model)
        {
            _model = model;
            SelectCounterViewModel.AvailableCounters.Clear();

            var availableCounter = new List<SelectCounterViewModel.AvailableCounterViewModel>();
            foreach (var counterPair in _model.Where(c => !SelectedCounterKeys.Contains(c.Key)))
                availableCounter.Add(new SelectCounterViewModel.AvailableCounterViewModel(counterPair.Key, counterPair.Value.DisplayName));

            SelectCounterViewModel.AvailableCounters = new ObservableCollection<SelectCounterViewModel.AvailableCounterViewModel>(availableCounter.OrderBy(c => c.Key));
            SelectCounterViewModel.TrySelectFirstCounter();
            
            foreach (var counterViewModel in SelectedCounterViewModels)
            {
                if (!_model.TryGetValue(counterViewModel.Key, out var counterModel))
                    continue;

                counterViewModel.Model = counterModel;
            }
            
            UpdatePoints();
            IsAutoFitEnabled = true;
        }

        public void SelectCurrentCounter()
        {
            var counterToAdd = _model[SelectCounterViewModel.SelectedCounter];
            SelectCounter(counterToAdd.Name, counterToAdd.Name, new SolidColorBrush(SelectCounterViewModel.Color), SelectCounterViewModel.UnitsViewModel.SelectedDataUnits, SelectCounterViewModel.UnitsViewModel.SelectedViewUnits);
        }

        public void SelectCounter(string counterKey, string name, SolidColorBrush brush, Units dataUnits, Units viewUnits)
        {
            _model.TryGetValue(counterKey, out var counterModel);

            var counterVM = new SelectedCounterViewModel(
                counterModel,
                counterKey,
                name,
                brush,
                RemoveSelectedCounter,
                default,
                default,
                dataUnits,
                viewUnits);
            
            SelectedCounterViewModels.Add(counterVM);
            SelectedCounterKeys.Add(counterKey);
			
            SelectCounterViewModel.RemoveFromAvailabile(counterKey);
            SelectCounterViewModel.TrySelectFirstCounter();
            
            SelectCounterViewModel.Color = Color.FromRgb((byte)_rnd.Next(0, 255), (byte)_rnd.Next(0, 255), (byte)_rnd.Next(0, 255));
            IsAutoFitEnabled = true;
        }
        
        private void RemoveSelectedCounter(SelectedCounterViewModel counter)
        {
            SelectedCounterViewModels.Remove(counter);
			
            // if we have only layout without data
            if (!_model.TryGetValue(counter.Key, out var removedCounter))
                return;
            SelectCounterViewModel.AvailableCounters.Add(new SelectCounterViewModel.AvailableCounterViewModel(removedCounter.Name, removedCounter.DisplayName));
            SelectCounterViewModel.AvailableCounters = new ObservableCollection<SelectCounterViewModel.AvailableCounterViewModel>(SelectCounterViewModel.AvailableCounters.OrderBy(c => c.Key));
            if (SelectCounterViewModel.SelectedCounter == null)
                SelectCounterViewModel.TrySelectFirstCounter();
        }
        
        public void Clear()
        {
            foreach (var selectedCounterViewModel in _selectedCounterViewModels)
            {
                selectedCounterViewModel.Points.Clear();
                selectedCounterViewModel.HoverPoint = new Point();
                selectedCounterViewModel.HoverPointInScreenSpace = new Point();
            }

            _selectCounterViewModel.Clear();
        }

        /// <summary>
        /// Refresh counters points to current model
        /// </summary>
        private void UpdatePoints()
        {
            foreach (var counterViewModel in SelectedCounterViewModels)
                counterViewModel.UpdatePoints();
        }
        
        public void Zoom(double start, double duration)
        {
            var yMin = double.MaxValue;
            var yMax = double.MinValue;
            var yCalculated = false;
            foreach (var selectedCounterViewModel in SelectedCounterViewModels)
            {
                if (selectedCounterViewModel.Points.Count == 0)
                    continue;

                var xIndexLeft = GetPointIndex(selectedCounterViewModel.Points, start);
                var xIndexRight = GetPointIndex(selectedCounterViewModel.Points, start + duration);

                if (selectedCounterViewModel.Points.Count - 1 == xIndexLeft || xIndexRight == 0)
                {
                    // zoom area is after/before points
                    // i am not sure what to do in this situation
                }
                else
                {
                    var leftY = Interpolate(xIndexLeft, start, selectedCounterViewModel.Points);
                    yMin = Math.Min(yMin, leftY);
                    yMax = Math.Max(yMax, leftY);

                    var rightY = selectedCounterViewModel.Points.Count - 1 != xIndexRight
                        ? Interpolate(xIndexRight, start + duration, selectedCounterViewModel.Points)
                        : selectedCounterViewModel.Points[xIndexRight].Y;
                    yMin = Math.Min(yMin, rightY);
                    yMax = Math.Max(yMax, rightY);

                    // this points are inside interval
                    for (int xIndex = xIndexLeft + 1; xIndex <= xIndexRight; xIndex++)
                    {
                        var y = selectedCounterViewModel.Points[xIndex].Y;

                        yMin = Math.Min(yMin, y);
                        yMax = Math.Max(yMax, y);
                    }

                    yCalculated = true;
                }
            }

            XMin = start;
            Width = duration;
            
            if (yCalculated)
            {
                YMin = yMin;
                Height = yMax - yMin;
            }
        }
        
        private void SetAxis(string propertyName, bool skipHeight, ref double field, double value)
        {
            if (field == value)
                return;

            field = value;
            OnPropertyChanged(propertyName);

            if (!skipHeight)
            {
                var oldHeight = _height;
                _height = YMax - YMin;
                if (oldHeight != _height)
                    OnPropertyChanged(nameof(Height));
            }
        }
        
        private double Interpolate(int closestLeftIndex, double value, PointCollection points)
        {
            var startLeftPoint = points[closestLeftIndex];
            var startRightPoint = points[closestLeftIndex + 1];
            var startT = (value - startLeftPoint.X) / (startRightPoint.X - startLeftPoint.X);
            return startLeftPoint.Y + startT * (startRightPoint.Y - startLeftPoint.Y);
        }
        
        public static int GetPointIndex(PointCollection points, double plotX)
        {
            var minIntervalIndex = 0;
            var maxIntervalIndex = points.Count - 2;
            while (true)
            {
                if (maxIntervalIndex < 0)
                    return 0;

                // we are out of last interval bounds
                if (minIntervalIndex == points.Count - 1)
                    return points.Count - 1;
				
                var checkIntervalIndex = minIntervalIndex + (maxIntervalIndex - minIntervalIndex) / 2;

                var leftPoint = points[checkIntervalIndex];
                var rightPoint = points[checkIntervalIndex + 1];
                if (leftPoint.X <= plotX && plotX <= rightPoint.X)
                    // we take a left point as data
                    return checkIntervalIndex;

                if (leftPoint.X < plotX)
                {
                    // right point index
                    minIntervalIndex = checkIntervalIndex + 1;
                }
                else if (leftPoint.X > plotX)
                {
                    // left point index
                    maxIntervalIndex = checkIntervalIndex - 1;
                }
            }
        }
    }
}