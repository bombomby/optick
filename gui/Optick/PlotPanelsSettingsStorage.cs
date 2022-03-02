using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using Newtonsoft.Json;
using Profiler.ViewModels;
using Profiler.ViewModels.Plots;

namespace Profiler
{
    public static class PlotPanelsSettingsStorage
    {
        public static List<PlotPanelSerialized> Load(string path)
        {
            if (!File.Exists(path))
                return new List<PlotPanelSerialized>();

            using (var file = File.Open(path, FileMode.Open, FileAccess.Read))
            using (var reader = new StreamReader(file))
            {
                return JsonConvert.DeserializeObject<List<PlotPanelSerialized>>(reader.ReadToEnd()) ?? new List<PlotPanelSerialized>();
            }
        }

        public static void Save(List<PlotsViewModel> plotPanels, string path)
        {
            var customPlanesStorage = new List<PlotPanelSerialized>();
                
            foreach (var customPanel in plotPanels)
            {
                var serializedPlane = new PlotPanelSerialized
                {
                    Title = customPanel.Title,
                    Counters = new List<PlotPanelSerialized.CounterSerialized>(),
                };
                foreach (var selectedCounterViewModel in customPanel.SelectedCounterViewModels)
                {
                    var color = ((SolidColorBrush)selectedCounterViewModel.Color).Color;
                    serializedPlane.Counters.Add(new PlotPanelSerialized.CounterSerialized
                    {
                        Key = selectedCounterViewModel.Key,
                        Name = selectedCounterViewModel.Name,
                        Color = new PlotPanelSerialized.CounterSerialized.ColorSerialized
                        {
                            A = color.A,
                            R = color.R,
                            G = color.G,
                            B = color.B,
                        } ,
                        DataUnits = selectedCounterViewModel.DataUnits,
                        ViewUnits = selectedCounterViewModel.ViewUnits
                    });
                }
                
                customPlanesStorage.Add(serializedPlane);
            }

            var serializedSettings = JsonConvert.SerializeObject(customPlanesStorage);
            using (var file = File.Open(path, FileMode.Create))
            using (var writer = new StreamWriter(file))
                writer.Write(serializedSettings);
        }
    }
    
    public class PlotPanelSerialized
    {
        public string Title { get; set; }

        public List<CounterSerialized> Counters { get; set; }
        
        public class CounterSerialized
        {
            public string Key { get; set; }
            public string Name { get; set; }
            public ColorSerialized Color { get; set; }
            public Units DataUnits { get; set; }
            public Units ViewUnits { get; set; }
                
            public class ColorSerialized
            {
                public byte A { get; set; }
                public byte R { get; set; }
                public byte G { get; set; }
                public byte B { get; set; }
            }
        }
    }
}