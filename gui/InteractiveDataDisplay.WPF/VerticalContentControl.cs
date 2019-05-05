// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Media;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Provides control that presents its content rotated by 90 degrees counterclockwise
    /// </summary>
    [TemplatePart(Name = "Presenter", Type = typeof(ContentPresenter))]
    [Description("Presents content vertically")]
    public class VerticalContentControl : ContentControl
    {
        private FrameworkElement contentPresenter;

        /// <summary>
        /// Initializes new instance of <see cref="VerticalContentControl"/> class
        /// </summary>
        public VerticalContentControl()
        {
            DefaultStyleKey = typeof(VerticalContentControl);
        }

        /// <summary>
        /// Invoked whenever application code or internal processes call ApplyTemplate
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            contentPresenter = GetTemplateChild("Presenter") as FrameworkElement;
        }

        /// <summary>
        /// Positions child elements and determines a size for a VerticalContentControl
        /// </summary>
        /// <param name="finalSize">The final area within the parent that VerticalContentControl should use to arrange itself and its children</param>
        /// <returns>The actual size used</returns>
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (contentPresenter != null)
                contentPresenter.Arrange(new Rect(new Point(0, 0), new Size(finalSize.Height, finalSize.Width)));
            return finalSize;
        }

        /// <summary>
        /// Measures the size in layout required for child elements and determines a size for the VerticalContentControl. 
        /// </summary>
        /// <param name="availableSize">The available size that this element can give to child elements. Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size that this element determines it needs during layout, based on its calculations of child element sizes.</returns>
        protected override Size MeasureOverride(Size availableSize)
        {
            if (contentPresenter == null)
                return Size.Empty;
            contentPresenter.Measure(new Size(availableSize.Height, availableSize.Width));
            contentPresenter.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection(new Transform[] {
                    new RotateTransform { Angle = -90 },
                    new TranslateTransform { Y = contentPresenter.DesiredSize.Width }
                })
            };
            return new Size(contentPresenter.DesiredSize.Height, contentPresenter.DesiredSize.Width);
        }
    }
}