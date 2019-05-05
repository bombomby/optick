// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Control for placing legend contents.
    /// </summary>
    [Description("Control for legend contents")]    
    public class Legend : ContentControl
    {
        /// <summary>
        /// Gets or sets the number of items in legend. Value of this property
        /// is automatically updated set to item count if content is <see cref="LegendItemsPanel"/>.
        /// It is recommended to use this property as read only.
        /// </summary>
        [Browsable(false)]
        public int ItemsCount
        {
            get { return (int)GetValue(ItemsCountProperty); }
            private set { SetValue(ItemsCountProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="ItemsCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsCountProperty =
            DependencyProperty.Register("ItemsCount", typeof(int), typeof(Legend), new PropertyMetadata(0));

        /// <summary>
        /// This method is called when the content property changes.
        /// </summary>
        /// <param name="oldContent">Old value of property.</param>
        /// <param name="newContent">New value of property.</param>
        protected override void OnContentChanged(object oldContent, object newContent)
        {
            var panel = oldContent as Panel;
            if (panel != null)
                panel.LayoutUpdated -= OnContentLayoutUpdated;
            panel = newContent as Panel;
            if (panel != null)
                panel.LayoutUpdated += OnContentLayoutUpdated;
            UpdateItemsCount();
            base.OnContentChanged(oldContent, newContent);
        }

        private void OnContentLayoutUpdated(object sender, EventArgs e)
        {
            UpdateItemsCount();
        }

        /// <summary>
        /// Initializes a new instance of <see cref="Legend"/> class.
        /// </summary>
        public Legend()
        {
            DefaultStyleKey = typeof(Legend);
            IsTabStop = false;
        }

        /// <summary>
        /// This method is invoked whenever application code or internal processes call ApplyTemplate.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateItemsCount();
        }

        private void UpdateItemsCount()
        {
            var panel = Content as Panel;
            if (panel != null)
                ItemsCount = panel.Children.Count;
            else
                ItemsCount = Content != null ? 1 : 0;
        }

        /// <summary>
        /// Returns value of <see cref="IsVisibleProperty"/> attached property that controls 
        /// if object is visible in legend. Default value is true.
        /// </summary>
        /// <param name="obj">An object with a legend.</param>
        /// <returns>Visibility of specified object in legend.</returns>
        public static bool GetIsVisible(DependencyObject obj)
        {
            if (obj != null)
                return (bool)obj.GetValue(IsVisibleProperty);
            else
                return true;
        }

        /// <summary>
        /// Sets <see cref="IsVisibleProperty"/> attached property. Objects with false value 
        /// of this property are not shown in legend.</summary>
        /// <param name="obj">An object to be presented in a legend.</param>
        /// <param name="isVisible">Legend visibility flag</param>
        public static void SetIsVisible(DependencyObject obj, bool isVisible)
        {
            if (obj != null)
                obj.SetValue(IsVisibleProperty, isVisible);
        }

        /// <summary>
        /// Identifies attached property to get or set visibility of a legend.
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(Legend), new PropertyMetadata(true));        
    }
}

