// Copyright © Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reactive.Subjects;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>Represents a base for all elements that perform graphic plotting. Handles coordinate
    /// transformations and manages composition of plots</summary>
    public abstract class PlotBase : Panel
    {
        #region Fields

        private double scaleXField;
        private double scaleYField;
        private double offsetXField;
        private double offsetYField;

        private bool isInternalChange = false;

        private DataRect actualPlotRect = new DataRect(0, 0, 1, 1);

        #endregion

        #region Properties

        /// <summary>Gets or sets padding - distance in screen units from each side of border to edges of plot bounding rectangle. 
        /// Effective padding for composition of plots is computed as maximum of all paddings.</summary>
        [Category("InteractiveDataDisplay")]
        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <summary>
        /// Desired ratio of horizontal scale to vertical scale. Values less than or equal to zero 
        /// represent unspecified aspect ratio.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public double AspectRatio
        {
            get { return (double)GetValue(AspectRatioProperty); }
            set { SetValue(AspectRatioProperty, value); }
        }

        /// <summary>
        /// Gets or sets auto fit mode. <see cref="IsAutoFitEnabled"/> property of all
        /// plots in compositions are updated instantly to have same value.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsAutoFitEnabled
        {
            get { return (bool)GetValue(IsAutoFitEnabledProperty); }
            set { SetValue(IsAutoFitEnabledProperty, value); }
        }

        /// <summary>Gets or sets desired plot width in plot coordinates. <see cref="PlotWidth"/> property of all
        /// plots in compositions are updated instantly to have same value. Settings this property
        /// turns off auto fit mode.</summary>
        [Category("InteractiveDataDisplay")]
        public double PlotWidth
        {
            get { return (double)GetValue(PlotWidthProperty); }
            set { SetValue(PlotWidthProperty, value); }
        }

        /// <summary>Gets or sets desired plot height in plot coordinates. <see cref="PlotHeight"/> property of all
        /// plots in compositions are updated instantly to have same value. Settings this property
        /// turns off auto fit mode.</summary>    
        [Category("InteractiveDataDisplay")]
        public double PlotHeight
        {
            get { return (double)GetValue(PlotHeightProperty); }
            set { SetValue(PlotHeightProperty, value); }
        }

        /// <summary>Gets or sets desired minimal visible horizontal coordinate (in plot coordinate system). 
        /// <see cref="PlotOriginX"/> property of all
        /// plots in compositions are updated instantly to have same value. Settings this property
        /// turns off auto fit mode.</summary>
        [Category("InteractiveDataDisplay")]
        public double PlotOriginX
        {
            get { return (double)GetValue(PlotOriginXProperty); }
            set { SetValue(PlotOriginXProperty, value); }
        }

        /// <summary>Gets or sets desired minimal visible vertical coordinate (in plot coordinate system). 
        /// <see cref="PlotOriginY"/> property of all
        /// plots in compositions are updated instantly to have same value. Settings this property
        /// turns off auto fit mode.</summary>
        [Category("InteractiveDataDisplay")]
        public double PlotOriginY
        {
            get { return (double)GetValue(PlotOriginYProperty); }
            set { SetValue(PlotOriginYProperty, value); }
        }

        /// <summary>
        /// Identifies <see cref="IsXAxisReversed"/> dependency property
        /// </summary>
        public static readonly DependencyProperty IsXAxisReversedProperty =
            DependencyProperty.Register("IsXAxisReversed", typeof(bool), typeof(PlotBase), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a flag indicating whether the x-axis is reversed or not.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsXAxisReversed
        {
            get { return (bool)GetValue(IsXAxisReversedProperty); }
            set { SetValue(IsXAxisReversedProperty, value); }
        }

        /// <summary>
        /// Identifies <see cref="IsYAxisReversed"/> dependency property
        /// </summary>
        public static readonly DependencyProperty IsYAxisReversedProperty =
            DependencyProperty.Register("IsYAxisReversed", typeof(bool), typeof(PlotBase), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a flag indicating whether the y-axis is reversed or not.
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public bool IsYAxisReversed
        {
            get { return (bool)GetValue(IsYAxisReversedProperty); }
            set { SetValue(IsYAxisReversedProperty, value); }
        }

        /// <summary>Gets or sets transform from user data to horizontal plot coordinate. 
        /// By default transform is <see cref="IdentityDataTransform"/>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTransform XDataTransform
        {
            get { return (DataTransform)GetValue(XDataTransformProperty); }
            set { SetValue(XDataTransformProperty, value); }
        }

        /// <summary>Gets or sets transform from user data to vertical plot coordinate. 
        /// By default transform is <see cref="IdentityDataTransform"/>
        /// </summary>
        [Category("InteractiveDataDisplay")]
        public DataTransform YDataTransform
        {
            get { return (DataTransform)GetValue(YDataTransformProperty); }
            set { SetValue(YDataTransformProperty, value); }
        }

        /// <summary>
        /// Gets or Sets horizontal scale parameter for plot transform
        /// </summary>
        public double ScaleX
        {
            get
            {
                return IsMaster ? scaleXField : masterField.ScaleX;
            }
            protected set
            {
                scaleXField = value;
            }
        }

        /// <summary>
        /// Gets or sets Horizontal offset for plot transform
        /// </summary>
        public double OffsetX
        {
            get
            {
                return IsMaster ? offsetXField : masterField.OffsetX;
            }
            protected set
            {
                offsetXField = value;
            }
        }

        /// <summary>
        /// Gets or Sets horizontal scale parameter for plot transform
        /// </summary>
        public double ScaleY
        {
            get
            {
                return IsMaster ? scaleYField : masterField.ScaleY;
            }
            protected set
            {
                scaleYField = value;
            }
        }

        /// <summary>
        /// Gets or sets Horizontal offset for plot transform
        /// </summary>
        public double OffsetY
        {
            get
            {
                return IsMaster ? offsetYField : masterField.OffsetY;
            }
            protected set
            {
                offsetYField = value;
            }
        }

        /// <summary>Gets actual plot rectangle which is exactly corresponds to visible part of plot</summary>
        public DataRect ActualPlotRect
        {
            get { return IsMaster ? actualPlotRect : masterField.ActualPlotRect; }
            private set { actualPlotRect = value; }
        }

        /// <summary>Gets desired plot rectangle. Actual plot rectangle may be larger because of aspect ratio 
        /// constraint</summary>
        public DataRect PlotRect
        {
            get { return new DataRect(PlotOriginX, PlotOriginY, PlotOriginX + PlotWidth, PlotOriginY + PlotHeight); }
        }

        private bool IsInternalChange
        {
            get { return IsMaster ? isInternalChange : masterField.IsInternalChange; }
            set
            {
                if (IsMaster)
                    isInternalChange = value;
                else
                    masterField.IsInternalChange = value;
            }
        }



        #endregion

        /// <summary>
        /// Initializes new instance of <see cref="PlotBase"/> class
        /// </summary>
        protected PlotBase()
        {
            XDataTransform = new IdentityDataTransform();
            YDataTransform = new IdentityDataTransform();
            masterField = this;
            Loaded += PlotBaseLoaded;
            Unloaded += PlotBaseUnloaded;
        }


        #region dependants tree

        private List<PlotBase> dependantsField = new List<PlotBase>();
        /// <summary>
        /// Master PlotBase of current PlotBase
        /// </summary>
        protected PlotBase masterField;

        private void AddDependant(PlotBase dependant)
        {
            if (!dependantsField.Contains(dependant))
            {
                dependantsField.Add(dependant);
                EnumAll(plot => plot.NotifyCompositionChange());
          }
        }

        private void RemoveDependant(PlotBase dependant)
        {
            if (dependantsField.Contains(dependant))
            {
                dependantsField.Remove(dependant);
                EnumAll(plot => plot.NotifyCompositionChange());
            }
        }

        private void connectMaster()
        {
            if (!forceMasterField)
            {
                var element = VisualTreeHelper.GetParent(this);
                while (element != null && !(element is PlotBase))
                    element = VisualTreeHelper.GetParent(element);
                if (element != null)
                {
                    var newMaster = (PlotBase)element;

                    // Take plot-related properties from new master plot
                    IsInternalChange = true;
                    PlotOriginX = newMaster.PlotOriginX;
                    PlotOriginY = newMaster.PlotOriginY;
                    PlotWidth = newMaster.PlotWidth;
                    PlotHeight = newMaster.PlotHeight;
                    IsAutoFitEnabled = newMaster.IsAutoFitEnabled;
                    IsInternalChange = false;

                    masterField = newMaster;
                    masterField.AddDependant(this);
                    InvalidateBounds();
                }
            }
        }

        private void disconnectMaster()
        {
            if (masterField != this)
            {
                masterField.RemoveDependant(this);
                InvalidateBounds();
                masterField = this;
            }
        }

        private void EnumLeaves(Action<PlotBase> action)
        {
            action(this);
            foreach (var item in dependantsField)
            {
                item.EnumLeaves(action);
            }
        }

        private void EnumAll(Action<PlotBase> action)
        {
            if (masterField != this)
            {
                if(masterField != null)
                    masterField.EnumAll(action);
            }
            else
                EnumLeaves(action);
        }

        bool forceMasterField = false;

        /// <summary>
        /// Disables or enables the connection of the panel to its parent context.
        /// </summary>
        /// <remarks>
        /// The default value is false which allows automatically connecting to a parent master and switching to a dependant mode. 
        /// If set to true the connection is disabled and the pannel is always in a master mode.
        /// </remarks>
        public bool ForceMaster
        {
            get { return forceMasterField; }
            set
            {
                forceMasterField = value;
                if (forceMasterField)
                    disconnectMaster();
                else
                    connectMaster();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the panel is a master mode or in a dependant mode.
        /// </summary>
        public bool IsMaster { get { return masterField == this; } }

        private IEnumerable<PlotBase> SelfAndAllDependantPlots
        {
            get
            {
                IEnumerable<PlotBase> result = new PlotBase[] { this };
                foreach (var item in dependantsField)
                {
                    result = result.Concat(item.SelfAndAllDependantPlots);
                }
                return result;
            }
        }

        /// <summary>
        /// Gets collections of all related plots of current in composition
        /// </summary>
        public IEnumerable<PlotBase> RelatedPlots
        {
            get
            {
                if (masterField != this)
                    return masterField.RelatedPlots;
                else
                {
                    IEnumerable<PlotBase> result = new PlotBase[] { this };
                    foreach (var item in dependantsField)
                    {
                        result = result.Concat(item.SelfAndAllDependantPlots);
                    }
                    return result;
                }
            }
        }

        private Subject<PlotCompositionChange> compositionChange = new Subject<PlotCompositionChange>();

        /// <summary>
        /// Raises plots composition changed event
        /// </summary>
        protected void NotifyCompositionChange()
        {
            compositionChange.OnNext(new PlotCompositionChange());
        }

        /// <summary>
        /// Gets event which occures when plots composition is changed
        /// </summary>
        public IObservable<PlotCompositionChange> CompositionChange
        {
            get { return compositionChange; }
        }

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies <see cref=" XDataTransform"/> dependency property
        /// </summary>
        public static readonly DependencyProperty XDataTransformProperty =
            DependencyProperty.Register("XDataTransform", typeof(DataTransform), typeof(PlotBase), new PropertyMetadata(null,
                (o, e) =>
                {
                    Plot plot = o as Plot;
                    if (plot != null)
                    {
                        plot.OnXDataTransformChanged(e);
                    }
                }));


        /// <summary>
        /// Identifies <see cref=" YDataTransform"/> dependency property
        /// </summary>
        public static readonly DependencyProperty YDataTransformProperty =
            DependencyProperty.Register("YDataTransform", typeof(DataTransform), typeof(PlotBase), new PropertyMetadata(null,
                (o, e) =>
                {
                    Plot plot = o as Plot;
                    if (plot != null)
                    {
                        plot.OnYDataTransformChanged(e);
                    }
                }));

        /// <summary>
        /// Identifies <see cref="Padding"/> dependency property
        /// </summary>
        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register("Padding", typeof(Thickness), typeof(PlotBase), new PropertyMetadata(new Thickness(0.0),
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        if (!plotBase.IsMaster)
                        {
                            plotBase.masterField.InvalidateMeasure();
                        } 
                        else
                            plotBase.InvalidateMeasure();
                    }
                }));

        /// <summary>
        /// Identifies <see cref="AspectRatio"/> dependency property
        /// </summary>
        public static readonly DependencyProperty AspectRatioProperty =
            DependencyProperty.Register("AspectRatio", typeof(double), typeof(PlotBase), new PropertyMetadata(0.0,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.InvalidateMeasure();
                        if (!plotBase.IsMaster)
                        {
                            plotBase.masterField.InvalidateMeasure();
                        }
                    }
                }));

        /// <summary>
        /// Identifies <see cref="IsAutoFitEnabled"/> dependency property
        /// </summary>
        public static readonly DependencyProperty IsAutoFitEnabledProperty =
            DependencyProperty.Register("IsAutoFitEnabled", typeof(bool), typeof(PlotBase), new PropertyMetadata(true,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.OnIsAutoFitEnabledChanged(e);
                    }
                }));

        /// <summary>
        /// Identifies <see cref="PlotWidth"/> dependency property
        /// </summary>
        public static readonly DependencyProperty PlotWidthProperty =
            DependencyProperty.Register("PlotWidth", typeof(double), typeof(PlotBase), new PropertyMetadata(1.0,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.OnPlotWidthChanged(e);
                    }
                }));

        /// <summary>
        /// Identifies <see cref="PlotHeight"/> dependency property
        /// </summary>
        public static readonly DependencyProperty PlotHeightProperty =
            DependencyProperty.Register("PlotHeight", typeof(double), typeof(PlotBase), new PropertyMetadata(1.0,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.OnPlotHeightChanged(e);
                    }
                }));

        /// <summary>
        /// Identifies <see cref="PlotOriginX"/> dependency property
        /// </summary>
        public static readonly DependencyProperty PlotOriginXProperty =
            DependencyProperty.Register("PlotOriginX", typeof(double), typeof(PlotBase), new PropertyMetadata(0.0,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.OnPlotOriginXChanged(e);
                    }
                }));

        /// <summary>
        /// Identifies <see cref="PlotOriginY"/> dependency property
        /// </summary>
        public static readonly DependencyProperty PlotOriginYProperty =
            DependencyProperty.Register("PlotOriginY", typeof(double), typeof(PlotBase), new PropertyMetadata(0.0,
                (o, e) =>
                {
                    PlotBase plotBase = (PlotBase)o;
                    if (plotBase != null)
                    {
                        plotBase.OnPlotOriginYChanged(e);
                    }
                }));

        /// <summary>Enables or disables clipping of graphic elements that are outside plotting area</summary>
        public bool ClipToBounds
        {
            get { return (bool)GetValue(ClipToBoundsProperty); }
            set { SetValue(ClipToBoundsProperty, value); }
        }

        /// <summary>Identifies <see cref="ClipToBounds"/> dependency property</summary>
        public static readonly DependencyProperty ClipToBoundsProperty =
            DependencyProperty.Register("ClipToBounds", typeof(bool), typeof(PlotBase), new PropertyMetadata(true,
                (s, a) => ((PlotBase)s).OnClipToBoundsChanged(a)));

        /// <summary>
        /// Occurs when <see cref="ClipToBounds"/> property changed
        /// </summary>
        /// <param name="args">PropertyChanged parameters</param>
        protected virtual void OnClipToBoundsChanged(DependencyPropertyChangedEventArgs args)
        {
            InvalidateMeasure(); 
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handler for <see cref="PlotOriginX"/> property changes
        /// </summary>
        protected virtual void OnPlotOriginXChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsInternalChange)
            {
                IsInternalChange = true;
                EnumAll(p => 
                { 
                    p.PlotOriginX = (double)e.NewValue;
                    p.IsAutoFitEnabled = false;
                    p.InvalidateMeasure();
                });
                IsInternalChange = false;
            }  
        }

        /// <summary>
        /// Handler for <see cref="PlotOriginY"/> property changes
        /// </summary>
        protected virtual void OnPlotOriginYChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsInternalChange)
            {
                IsInternalChange = true;
                EnumAll(p =>
                {
                    p.PlotOriginY = (double)e.NewValue;
                    p.IsAutoFitEnabled = false;
                    p.InvalidateMeasure();
                });
                IsInternalChange = false;
            }
        }

        /// <summary>
        /// Handler for <see cref="PlotWidth"/> property changes
        /// </summary>
        protected virtual void OnPlotWidthChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsInternalChange)
            {
                IsInternalChange = true;
                EnumAll(p =>
                {
                    p.PlotWidth = (double)e.NewValue;
                    p.IsAutoFitEnabled = false;
                    p.InvalidateMeasure();
                });
                IsInternalChange = false;
            } 
        }

        /// <summary>
        /// Handler for <see cref="PlotHeight"/> property changes
        /// </summary>
        protected virtual void OnPlotHeightChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsInternalChange)
            {
                IsInternalChange = true;
                EnumAll(p =>
                {
                    p.PlotHeight = (double)e.NewValue;
                    p.IsAutoFitEnabled = false;
                    p.InvalidateMeasure();
                });
                IsInternalChange = false;
            } 
        }

        /// <summary>
        /// Handler for <see cref="XDataTransform"/> property changes
        /// </summary>
        protected virtual void OnXDataTransformChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateBounds();
        }

        /// <summary>
        /// Handler for <see cref="YDataTransform"/> property changes
        /// </summary>
        protected virtual void OnYDataTransformChanged(DependencyPropertyChangedEventArgs e)
        {
            InvalidateBounds();
        }

        private void OnIsAutoFitEnabledChanged(DependencyPropertyChangedEventArgs e)
        {
            if (!IsInternalChange)
            {
                IsInternalChange = true;
                EnumAll(p =>
                {
                    p.IsAutoFitEnabled = (bool)e.NewValue;
                    if (p.IsAutoFitEnabled)
                        p.InvalidateMeasure();
                });
                IsInternalChange = false;
            }
        }

        /// <summary>
        /// Gets the range of {x,y} plot coordinates that correspond to all elements produced by the plot.
        /// </summary>
        /// <returns>The <see cref="DataRect"/> structure that holds the ranges of {x} and {y} plot coordinates or the <see cref="DataRect.Empty"/> value.</returns>
        protected virtual DataRect ComputeBounds()
        {
            return DataRect.Empty;
        }

        /// <summary>
        /// Invalidates effective plot coordinate ranges. This usually schedules recalculation of plot layout.
        /// </summary>
        public virtual void InvalidateBounds()
        {
            EnumAll(p => p.InvalidateMeasure());
        }

        /// <summary>
        /// A common padding used by all graphing elements in the dependant tree.
        /// </summary>
        /// <remarks>This is computed by traversing the master-dependant tree and computing the maximum mapping.</remarks>
        public Thickness ActualPadding { get { return IsMaster ? AggregatePadding() : masterField.ActualPadding; } }

        private double GetEffectiveAspectRatio()
        {
            double result = AspectRatio;
            for (int i = 0; i < dependantsField.Count && result <= 0; i++)
                result = dependantsField[i].GetEffectiveAspectRatio();
            return result;
        }

        /// <summary>
        /// Computes padding of current <see cref="PlotBase"/> instance
        /// </summary>
        /// <returns>Padding of current <see cref="PlotBase"/> instance</returns>
        protected virtual Thickness ComputePadding()
        {
            return Padding;
        }

        /// <summary>
        /// Computes padding with maximum values for each side from padding of current <see cref="PlotBase"/> instance and of all child <see cref="PlotBase"/> instances
        /// </summary>
        /// <returns>Padding with maximum values for each side from padding of current <see cref="PlotBase"/> instance and of all child <see cref="PlotBase"/> instances</returns>
        protected virtual Thickness AggregatePadding()
        {
            Thickness result = ComputePadding();
            foreach (var item in dependantsField)
            {
                Thickness their = item.AggregatePadding();
                if (their.Left > result.Left) result.Left = their.Left;
                if (their.Right > result.Right) result.Right = their.Right;
                if (their.Top > result.Top) result.Top = their.Top;
                if (their.Bottom > result.Bottom) result.Bottom = their.Bottom;
            }
            return result;
        }

        /// <summary>
        /// Computes minimal plot rectangle which contains plot rectangles of current <see cref="PlotBase"/> instance and of all child <see cref="PlotBase"/> instances
        /// </summary>
        /// <returns>Minimal plot rectangle which contains plot rectangles of current <see cref="PlotBase"/> instance and of all child <see cref="PlotBase"/> instances</returns>
        protected DataRect AggregateBounds()
        {
            DataRect result = ComputeBounds();
            foreach (var item in dependantsField)
                result.Surround(item.AggregateBounds());

            return result;
        }

        /// <summary>This event is fired when transform from plot to screen coordinates changes.</summary>
        public event EventHandler PlotTransformChanged;

        /// <summary>
        /// For a given range of {x,y} plot coordinates and a given screen size taking into account effective aspect ratio and effective padding computes navigation transform.
        /// </summary>
        /// <param name="plotBounds">The range of {x,y} plot coordinates that must fit into the screen.</param>
        /// <param name="screenSize">The width and height of the screen that must fit plotBounds.</param>
        internal void Fit(DataRect plotBounds, Size screenSize)
        {
            var padding = AggregatePadding();
            var screenWidth = Math.Max(1.0, screenSize.Width - padding.Left - padding.Right);
            var screenHeight = Math.Max(1.0, screenSize.Height - padding.Top - padding.Bottom);
            var plotWidth = plotBounds.X.IsEmpty ? 0.0 : plotBounds.X.Max - plotBounds.X.Min;
            if (plotWidth <= 0) plotWidth = 1.0;
            var plotHeight = plotBounds.Y.IsEmpty ? 0.0 : plotBounds.Y.Max - plotBounds.Y.Min;
            if (plotHeight <= 0) plotHeight = 1.0;

            ScaleX = screenWidth / plotWidth;
            ScaleY = screenHeight / plotHeight;

            var aspect = GetEffectiveAspectRatio();
            if (aspect > 0)
            {
                if (aspect * ScaleY < ScaleX)
                    ScaleX = aspect * ScaleY;
                else
                    ScaleY = ScaleX / aspect;
            }

            OffsetX = plotBounds.X.IsEmpty ? 0.0 : ((plotBounds.X.Min + plotBounds.X.Max) * ScaleX - screenWidth) * 0.5 - padding.Left;
            OffsetY = plotBounds.Y.IsEmpty ? screenSize.Height : ((plotBounds.Y.Min + plotBounds.Y.Max) * ScaleY + screenHeight) * 0.5 + padding.Top;

            ActualPlotRect = new DataRect(
                XFromLeft(screenSize.Width > padding.Left + padding.Right ? 0 : screenSize.Width * 0.5 - 0.5),
                YFromTop(screenSize.Height > padding.Top + padding.Bottom ? screenSize.Height : screenSize.Height * 0.5 + 0.5),
                XFromLeft(screenSize.Width > padding.Left + padding.Right ? screenSize.Width : screenSize.Width * 0.5 + 0.5),
                YFromTop(screenSize.Height > padding.Top + padding.Bottom ? 0 : screenSize.Height * 0.5 - 0.5));

            EnumAll(p =>
            {
                if (p.PlotTransformChanged != null)
                    p.PlotTransformChanged(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Sets plot rectangle for current instance of <see cref="PlotBase"/>
        /// </summary>
        /// <param name="plotRect">plot rectangle value that would be set for current instance of <see cref="PlotBase"/></param>
        /// <param name="fromAutoFit">Identifies that it is internal call</param>
        protected void SetPlotRect(DataRect plotRect, bool fromAutoFit)
        {
            IsInternalChange = true;

            EnumAll(p =>
            {
                p.PlotOriginX = plotRect.XMin;
                p.PlotOriginY = plotRect.YMin;
                p.PlotWidth = plotRect.Width;
                p.PlotHeight = plotRect.Height;
                if (!fromAutoFit)
                {
                    p.IsAutoFitEnabled = false;
                    p.InvalidateMeasure();
                }
            });
                 
            IsInternalChange = false;
        }

        /// <summary>
        /// Sets plot rectangle for current instance of <see cref="PlotBase"/>
        /// </summary>
        /// <param name="plotRect">plot rectangle value that would be set for current instance of <see cref="PlotBase"/></param>
        public void SetPlotRect(DataRect plotRect)
        {
            SetPlotRect(plotRect, false);
        }

        /// <summary>
        /// Performs horizontal transform from plot coordinates to screen coordinates
        /// </summary>
        /// <param name="x">Value in plot coordinates</param>
        /// <returns>Value in screen coordinates</returns>
        public double LeftFromX(double x)
        {
            return ScaleX > 0 ? x * ScaleX - OffsetX : x;
        }

        /// <summary>
        /// Performs vertical transform from plot coordinates to screen coordinates
        /// </summary>
        /// <param name="y">Value in plot coordinates</param>
        /// <returns>Value in screen coordinates</returns>
        public double TopFromY(double y)
        {
            return ScaleY > 0 ? OffsetY - y * ScaleY : OffsetY - y;
        }

        /// <summary>
        /// Horizontal transform from screen coordinates to plot coordinates
        /// </summary>
        /// <param name="left">Value in screen coordinates</param>
        /// <returns>Value in plot coordinates</returns>
        public double XFromLeft(double left)
        {
            return ScaleX > 0 ? (left + OffsetX) / ScaleX : left;
        }

        /// <summary>
        /// Vertical transform from screen coordinates to plot coordinates
        /// </summary>
        /// <param name="top">Value in screen coordinates</param>
        /// <returns>Value in plot coordinates</returns>
        public double YFromTop(double top)
        {
            return ScaleY > 0 ? (OffsetY - top) / ScaleY : OffsetY - top;
        }

        /// <summary>
        /// Finds master plot for specifyied element
        /// </summary>
        /// <param name="element">Element, which mater plot should be found</param>
        /// <returns>Master Plot for specified element</returns>
        public static PlotBase FindMaster(DependencyObject element)
        {
            var result = VisualTreeHelper.GetParent(element);

            while (result != null && !(result is PlotBase))
                result = VisualTreeHelper.GetParent(result);

            return result as PlotBase;
        }

        /// <summary>
        /// Occurs when PlotBase is loaded
        /// </summary>
        protected virtual void PlotBaseLoaded(object sender, RoutedEventArgs e)
        {
            connectMaster();
        }

        /// <summary>
        /// Occurs when PlotBase is unloaded
        /// </summary>
        protected virtual void PlotBaseUnloaded(object sender, RoutedEventArgs e)
        {
            disconnectMaster();
        }

        /// <summary>
        /// Performs measure algorithm if current instance of <see cref="PlotBase"/> is master
        /// </summary>
        /// <param name="availableSize">Availible size for measure</param>
        /// <returns>Desired size for current plot</returns>
        protected Size PerformAsMaster(Size availableSize)
        {
            if (double.IsNaN(availableSize.Width)
                || double.IsNaN(availableSize.Height)
                || double.IsInfinity(availableSize.Width)
                || double.IsInfinity(availableSize.Height)) 
                availableSize = new Size(100, 100);
            if (IsMaster)
            {
                DataRect desiredRect;
                if (IsAutoFitEnabled)
                {
                    desiredRect = AggregateBounds();
                    if (desiredRect.IsEmpty)
                        desiredRect = new DataRect(0, 0, 1, 1);

                    SetPlotRect(desiredRect, true);
                }
                else // resize
                    desiredRect = PlotRect;
                Fit(desiredRect, availableSize);
            }
            return availableSize;
        }

        #endregion


    }
}

