using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Profiler.InfrastructureMvvm
{
    /// <summary>
    /// A content control presenting a view for a given view model via binding.
    /// </summary>
    public class ViewModelPresenter : ContentControl
    {
        /// <summary>
        /// The view model for which this control should display the corresponding view.
        /// </summary>
        public object ViewModel
        {
            get { return GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register("ViewModel", typeof(object), typeof(ViewModelPresenter),
        new PropertyMetadata(default(object), OnViewModelChanged));

        private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (DesignerProperties.GetIsInDesignMode(d))
            {
                return;
            }

            var self = (ViewModelPresenter)d;
            self.Content = null;

            if (e.NewValue != null)
            {
                var view = ViewLocator.GetViewForViewModel(e.NewValue);
                self.Content = view;
            }
        }
    }
}
