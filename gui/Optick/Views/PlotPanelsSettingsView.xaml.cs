using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Profiler.ViewModels;

namespace Profiler.Views
{
    public partial class PlotPanelsSettingsView : UserControl
    {
        public PlotPanelsSettingsView()
        {
            InitializeComponent();
        }

        public event Action<List<PlotPanelSerialized>> PlotPanelsSettingsLoaded;
        public event Action<string> CreatePanel; 

        private void SaveCurrent_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (PlotPanelsSettingsViewModel)DataContext;
            PlotPanelsSettingsStorage.Save(vm.CustomPanels, vm.CurrentPath);
        }
        
        private void SaveAs_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (PlotPanelsSettingsViewModel)DataContext;

            var saveFileDialog = new SaveFileDialog
            {
                AddExtension = true,
                Filter = "Plot panels settings (*.json)|*.json"
            };
            var result = saveFileDialog.ShowDialog();
            if (result != true)
                return;
            
            vm.CurrentPath = saveFileDialog.FileName;
            PlotPanelsSettingsStorage.Save(vm.CustomPanels, vm.CurrentPath);
        }

        private void Load_OnClick(object sender, RoutedEventArgs e)
        {
            var vm = (PlotPanelsSettingsViewModel)DataContext;
            
            var openFileDialog = new OpenFileDialog
            {
                Filter = "(*.json)|*.json"
            };
            var result = openFileDialog.ShowDialog();
            if (result != true)
                return;

            vm.CurrentPath = openFileDialog.FileName;
            var plotsSettings = PlotPanelsSettingsStorage.Load(vm.CurrentPath);
            PlotPanelsSettingsLoaded?.Invoke(plotsSettings);

        }

        private void CreatePanel_OnClick(object sender, RoutedEventArgs e)
        {
            CreatePanel?.Invoke(txtBxTitle.Text);
        }
    }
}