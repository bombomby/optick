using System;
using System.Windows;
using System.Windows.Controls;

namespace Profiler.Views
{
    /// <summary>
    /// Interaction logic for PlatformSelectorView.xaml
    /// </summary>
    public partial class PlatformSelectorView : UserControl
    {
        public PlatformSelectorView()
        {
            InitializeComponent();
        }
    }

    // Using for DataTemplate Combobox
    public class ComboBoxItemTemplateSelector : DataTemplateSelector
    {
        // Can set both templates from XAML
        public DataTemplate SelectedItemTemplate { get; set; }
        public DataTemplate ItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            bool selected = false;

            // container is the ContentPresenter
            FrameworkElement fe = container as FrameworkElement;
            if (fe != null)
            {
                DependencyObject parent = fe.TemplatedParent;
                if (parent != null)
                {
                    ComboBox cbo = parent as ComboBox;
                    if (cbo != null)
                        selected = true;
                }
            }

            if (selected)
                return SelectedItemTemplate;
            else
                return ItemTemplate;
        }
    }
}
