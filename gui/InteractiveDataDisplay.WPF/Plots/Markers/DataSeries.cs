// Copyright (c) Microsoft Corporation. All Rights Reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Windows;
using System.Collections.Generic;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Collections;
using System.Globalization;
using System.Diagnostics;
using System.ComponentModel;

namespace InteractiveDataDisplay.WPF
{
    /// <summary>
    /// Represents one data series (or variable).
    /// </summary>
    public class DataSeries : DependencyObject
    {
        /// <summary>
        /// Occurs when a value of <see cref="Data"/> property was changed.
        /// </summary>
        public event EventHandler DataChanged;

        private double minValue = Double.NaN;
        private double maxValue = Double.NaN;
        private Array cachedEnum; // Cached values from IEnumerable

        /// <summary>
        /// Gets or sets the unique key of data series. Data series is accessed by this key when
        /// drawing markers and composing a legend. 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the readable description of this data series. It is shown in legend by default.
        /// <para>Default value is null.</para>
        /// </summary>
        public string Description
        {
            get { return (string)GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, (string)value); }
        }

        /// <summary>
        /// Identifies the <see cref="Description"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(DataSeries),
            new PropertyMetadata(null, OnDescriptionPropertyChanged));

        private static void OnDescriptionPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var ds = sender as DataSeries;
            if (ds.DataChanged != null) ds.DataChanged(ds, new EventArgs());
        }

        /// <summary>
        /// Gets or sets the data source for data series. This can be IEnumerable or 1D array for vector data or scalar objects.
        /// <para>Default value is null.</para>
        /// </summary>
        public object Data
        {
            get { return GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="Data"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(DataSeries),
            new PropertyMetadata(OnDataPropertyChanged));

        private static void OnDataPropertyChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ((DataSeries)sender).Update();
        }

        /// <summary>
        /// A method to raise <see cref="DataChanged"/> event.
        /// </summary>
        protected void RaiseDataChanged()
        {
            if (DataChanged != null)
                DataChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Forces this data series to be updated. 
        /// Is usually called if items of data array were changed, but reference to array itself remained the same.
        /// </summary>
        public void Update()
        {
            minValue = Double.NaN;
            maxValue = Double.NaN;
            cachedEnum = null;
            First = FindFirstDataItem();

            RaiseDataChanged();
        }

        /// <summary>
        /// Gets the first value in data series.
        /// <para>Returns null if <see cref="Data"/> is null.</para>
        /// </summary>
        public object First
        {
            get { return (object)GetValue(FirstProperty); }
            internal set { SetValue(FirstProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="First"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FirstProperty =
            DependencyProperty.Register("First", typeof(object), typeof(DataSeries), new PropertyMetadata(null));

        /// <summary>
        /// Gets the first value in data series and converts it if <see cref="Converter"/> is specified.
        /// </summary>
        /// <returns>Converted first value.</returns>
        public object FindFirstDataItem()
        {
            object uc = GetFirstUnconvertedDataItem();
            if (uc != null && Converter != null)
                return Converter.Convert(uc, null, this, CultureInfo.InvariantCulture);
            else 
                return uc;
        }

        /// <summary>
        /// Converts the elements of an System.Collection.IEnumerable to System.Array.
        /// </summary>
        /// <param name="enumerable">An instance of System.Collection.IEnumerable class.</param>
        /// <returns>An instance of System.Array class.</returns>
        public static Array GetArrayFromEnumerable(IEnumerable enumerable)
        {
            return enumerable.Cast<object>().ToArray();
        }

        internal Array GetCachedEnumerable(IEnumerable ie)
        {
            if (cachedEnum == null)
                cachedEnum = GetArrayFromEnumerable(ie);
            return cachedEnum;
        }

        private object GetFirstUnconvertedDataItem()
        {
            Array a = Data as Array;
            if (a != null)
            {
                if (a.Length > 0)
                    return a.GetValue(0);
                else
                    return null; // Data series has zero length - no first item available
            }
            else
            {
                IEnumerable ie = Data as IEnumerable;
                if (ie != null && !(Data is string))
                {
                    IEnumerator ir = ie.GetEnumerator();
                    if (ir.MoveNext())
                        return ir.Current;
                    else
                        return null; // Data series has no elements - no first item available
                }
                else if (Data != null)
                    return Data;
                else
                    return null; // Data series is null - no first item available
            }
        }

        /// <summary>
        /// Returns the minimum value in data series if it is numeric or Double.NaN in other cases.
        /// </summary>
        public double MinValue
        {
            get 
            {
                if (Double.IsNaN(minValue))
                {
                    try
                    {
                        var a = Data as Array;
                        if (a == null)
                        {
                            var ie = Data as IEnumerable;
                            if(ie != null)
                                a = GetArrayFromEnumerable(ie);
                        }
                        if (a != null && !(Data is string))
                        {                            
                            double min = a.Length > 0 ? Convert.ToDouble(a.GetValue(0), CultureInfo.InvariantCulture) : Double.NaN;
                            double temp = 0;
                            foreach (object obj in a)
                            {
                                temp = Convert.ToDouble(obj, CultureInfo.InvariantCulture);
                                if (temp < min)
                                    min = temp;
                            }
                            minValue = min;
                        }
                        else
                        {
                            if (this.Data is Byte || this.Data is SByte ||
                                this.Data is Int16 || this.Data is Int32 || this.Data is Int64 ||
                                this.Data is UInt16 || this.Data is UInt32 || this.Data is UInt64 ||
                                this.Data is Single || this.Data is Double || this.Data is Decimal)
                            {
                                double d = Convert.ToDouble(this.Data, CultureInfo.InvariantCulture);
                                minValue = d;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Cannot find Min in " + this.Key + " DataSeries: " + exc.Message);
                        minValue = Double.NaN;
                    }                    
                }
                return minValue;
            }
        }

        /// <summary>
        /// Returns the maximum value in data series if it is numeric or Double.NaN in other cases.
        /// </summary>
        public double MaxValue
        {
            get
            {
                if (Double.IsNaN(maxValue))
                    try
                    {
                        Array a = Data as Array;
                        if (a == null)
                        {
                            var ie = Data as IEnumerable;
                            if(ie != null)
                                a = GetArrayFromEnumerable(ie);
                        }
                        if (a != null && !(Data is string))
                        {
                            double max = a.Length > 0 ? Convert.ToDouble(a.GetValue(0), CultureInfo.InvariantCulture) : Double.NaN;
                            double temp = 0;
                            foreach (object obj in a)
                            {
                                temp = Convert.ToDouble(obj, CultureInfo.InvariantCulture);
                                if (temp > max)
                                    max = temp;
                            }
                            maxValue = max;
                        }
                        else
                        {
                            if (this.Data is Byte || this.Data is SByte ||
                                this.Data is Int16 || this.Data is Int32 || this.Data is Int64 ||
                                this.Data is UInt16 || this.Data is UInt32 || this.Data is UInt64 ||
                                this.Data is Single || this.Data is Double || this.Data is Decimal)
                            {
                                double d = Convert.ToDouble(this.Data, CultureInfo.InvariantCulture);
                                maxValue = d;
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        Debug.WriteLine("Cannot find Max in " + this.Key + " DataSeries: " + exc.Message);
                    }
                return maxValue;
            }
        }

        /// <summary>
        /// Gets data series length (the length of <see cref="Data"/> if it is vector or one if it is scalar). 
        /// Returns 0 for series with null <see cref="Data"/> properties.
        /// </summary>
        public int Length
        {
            get
            {
                var arr = Data as Array;
                if (arr != null)
                    return arr.Length;
                else
                {
                    var ie = Data as IEnumerable;
                    if (ie != null && !(Data is string)) // String is also IEnumerable, but we tread them as scalars
                        return GetCachedEnumerable(ie).Length;
                    else
                        return (Data == null) ? (0) : (1);
                }
            }
        }

        /// <summary>
        /// Returns true if data series is scalar or false otherwise. 
        /// Returns true for series with null <see cref="Data"/> properties.
        /// </summary>
        public bool IsScalar
        {
            get
            {
                if (Data == null || Data is string)
                    return true;
                else if (Data is Array || Data is IEnumerable)
                    return false;
                else 
                    return true;
            }
        }

        /// <summary>
        /// Gets or sets the converter which is applied to all objects in data series before binding to marker template.
        /// May be null, which means identity conversion.
        /// </summary>
        public IValueConverter Converter { get; set; }

        /// <summary>
        /// Gets the marker graph than owns that series.
        /// </summary>
        public MarkerGraph Owner { get; internal set; }
    }

    /// <summary>
    /// Represents a collection of data series. 
    /// </summary>
    public class DataCollection : ObservableCollection<DataSeries>
    {
        private Dictionary<string, DataSeries> lookup;
        DynamicDataCollection dynamicCollection;
        bool isValid;
        int markerCount;
        
        /// <summary>
        /// Occurs when an item in the collection or entire collection changes.
        /// </summary>
        public event EventHandler<DataSeriesUpdatedEventArgs> DataSeriesUpdated;

        /// <summary>
        /// Initializes a new empty instance of <see cref="DataCollection"/> class.
        /// </summary>
        public DataCollection()
        { }

        /// <summary>
        /// Looks up for <see cref="DataSeries"/> with specified <see cref="Key"/>. If found assigns it to the 
        /// output parameter and returns true. Returns false if no series with specified name was found.
        /// </summary>
        /// <param name="name">The key of data series to search for.</param>
        /// <param name="result">Output parameter for found series.</param>
        /// <returns>True if data series was found or false otherwise.</returns>
        public bool TryGetValue(string name, out DataSeries result)
        {
            if (lookup == null)
            {
                lookup = new Dictionary<string,DataSeries>();
                foreach (var ds in this)
                    lookup.Add(ds.Key, ds);
            }
            return lookup.TryGetValue(name, out result);
        }

        /// <summary>
        /// Checks whether a collection contains <see cref="DataSeries"/> with specified <see cref="Key"/>.
        /// </summary>
        /// <param name="name">The key of data series.</param>
        /// <returns>True is data series is in collection or false otherwise.</returns>
        public bool ContainsSeries(string name)
        {
            DataSeries dummy;
            return TryGetValue(name, out dummy);
        }

        /// <summary>
        /// Gets the index of <see cref="DataSeries"/> with <see cref="Key"/> "X". If there is no one returns -1.
        /// </summary>
        /// <returns>Index of X <see cref="DataSeries"/> in current collection.</returns>
        public int XSeriesNumber
        {
            get
            {
                DataSeries dummy;
                TryGetValue("X", out dummy);
                if (dummy == null)
                    return -1;
                else
                    return this.IndexOf(dummy);
            }
        }

        /// <summary>
        /// Gets the first <see cref="DataSeries"/> in collection with specified <see cref="Key"/>.
        /// </summary>
        /// <param name="name">The key of data series.</param>
        /// <returns>The first data series in collection with specified key.</returns>
        public DataSeries this[string name]
        {
            get
            {
                DataSeries result;
                if (TryGetValue(name, out result))
                    return result;
                else
                    return null;
            }
        }

        /// <summary>
        /// Gets <see cref="DynamicDataCollection"/> with generated properties to get data series by key. 
        /// Used in bindings in legend templates.
        /// </summary>
        public DynamicDataCollection Emitted
        {
            get { return dynamicCollection; }
        }

        /// <summary>
        /// Return true if collection is valid and can be drawn, false otherwise.
        /// <remarks>
        /// Collection is valid if all the vector data of data series are of equal length and
        /// all data series have a defined unique key.
        /// </remarks>
        /// </summary>
        public bool IsValid
        {
            get { return isValid; }
        }

        /// <summary>
        /// Gets the count of markers to draw.
        /// <remarks>
        /// If any data of data series in collection is a vector then the count of markers is vector's length. 
        /// Otherwise only one marker will be drawn.
        /// If any data series (except for data series with key "X") has null data then no markers will be drawn.
        /// </remarks>
        /// </summary>
        public int MarkerCount
        {
            get { return markerCount; }
        }

        /// <summary>
        /// Raises the <see cref="DataCollection.DataSeriesUpdated"/> event with the provided event data.
        /// </summary>
        /// <param name="e">Provided data for collection changing event.</param>
        protected override void OnCollectionChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Type dynamic = DynamicTypeGenerator.GenerateDataCollectionType(this);
            dynamicCollection = Activator.CreateInstance(dynamic, this) as DynamicDataCollection;

            CheckDataCollection();
            base.OnCollectionChanged(e);
            if (e == null)
            {
                throw new ArgumentNullException("e");
            }
            if (e.OldItems != null)
                foreach (DataSeries item in e.OldItems)
                    item.DataChanged -= OnSeriesDataChanged;
            if (e.NewItems != null)
                foreach (DataSeries item in e.NewItems)
                    item.DataChanged += OnSeriesDataChanged;
            lookup = null;

            OnPropertyChanged(new PropertyChangedEventArgs("Emitted"));
        }

        private void OnSeriesDataChanged(object sender, EventArgs e)
        {
            CheckDataCollection();
            if (DataSeriesUpdated != null)
            {
                var ds = sender as DataSeries;
                if (ds != null)
                    DataSeriesUpdated(this, new DataSeriesUpdatedEventArgs(ds.Key));
            }
            lookup = null;
        }

        private void CheckDataCollection()
        {
            int n = 0;
            isValid = true;
            
            var vs = this.Where(s => !s.IsScalar).FirstOrDefault();
            if (vs != null)
                n = vs.Length; // Take length of first vector series
            else
            {
                var ns = this.Where(s => s.Data != null).FirstOrDefault(); // Find any scalar series with non-null data
                if (ns != null)
                    n = 1;                
            }

            var ser = this.Where(s => (s.Data == null && s.Key != "X")).FirstOrDefault();
            if (ser == null)
                markerCount = n;
            else 
                markerCount = 0;
            
            foreach (DataSeries ds in this)
            {
                if (!ds.IsScalar && n != ds.Length)
                    isValid = false;

                if (String.IsNullOrEmpty(ds.Key))
                    isValid = false;
                
                for (int i = 0; i < this.Count; i++)
                    if (this[i] != ds && this[i].Key == ds.Key)
                    {
                        this.Remove(ds);
                        isValid = false;
                    }
            }
        }
    }

    /// <summary>
    /// Provides event data for the <see cref="DataCollection.DataSeriesUpdated"/> event.
    /// </summary>
    public class DataSeriesUpdatedEventArgs : EventArgs
    {
        private string key;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataSeriesUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="key">The <see cref="DataSeries.Key"/> of the updated <see cref="DataSeries"/>.</param>
        public DataSeriesUpdatedEventArgs(string key)
        {
            this.key = key;
        }

        /// <summary>
        /// Gets the <see cref="DataSeries.Key"/> of the updated <see cref="DataSeries"/>.
        /// </summary>
        public string Key { get { return key; } }
    }

    /// <summary>
    /// Provides an access to all the information about one specific marker.
    /// </summary>
    public class MarkerViewModel : INotifyPropertyChanged
    {
        int row;
        bool useConverters = true;
        DataCollection dataCollection;
        MarkerViewModel sourceModel;

        /// <summary>
        /// Initializes a new instance of <see cref="MarkerViewModel"/> class that uses converters.
        /// </summary>
        /// <param name="row">The number of marker.</param>
        /// <param name="collection">A collection of data series.</param>
        public MarkerViewModel(int row, DataCollection collection) : 
            this(row, collection, true)
        { /* Nothing to do here */ }

        /// <summary>
        /// Initializes a new instance of <see cref="MarkerViewModel"/> class.
        /// </summary>
        /// <param name="row">The number of marker.</param>
        /// <param name="collection">A collection of data series.</param>
        /// <param name="converters">A value indicating should converters be used or not.</param>
        protected MarkerViewModel(int row, DataCollection collection, bool converters)
        {
            this.row = row;
            this.dataCollection = collection;
            this.useConverters = converters;
        }

        /// <summary>
        /// Gets the <see cref="DynamicDataCollection"/> (a specially created wrapper for <see cref="DataCollection"/>)
        /// this instance of <see cref="MarkerViewModel"/> class is associated with.
        /// </summary>
        public DynamicDataCollection Series
        {
            get { return dataCollection.Emitted; }
        }
        
        /// <summary>
        /// Gets a new instance of <see cref="MarkerViewModel"/> class with the same fields but which doesn't use converters.
        /// </summary>
        public MarkerViewModel Sources
        {
            get
            {
                if (sourceModel == null)
                    sourceModel = new MarkerViewModel(row, dataCollection, false);
                return sourceModel;
            }
        }
        internal int Row
        {
            get { return row; }
            set 
            {
                row = value;
                if (sourceModel != null)
                    sourceModel.Row = value;
            }
        }
        /// <summary>
        /// Returns the value of a specific property.
        /// </summary>
        /// <param name="name">The <see cref="DataSeries.Key"/> of data series associated with the property.</param>
        /// <returns></returns>
        public object this[string name]
        {
            get
            {
                // Find data series by name
                var dataSeries = dataCollection[name];
                if (dataSeries == null || dataSeries.Data == null)
                    if(name == "X")
                        return row;
                    else
                        return null;

                // Get value
                object value = null;
                var arr = dataSeries.Data as Array;
                if (arr != null)
                {
                    if(row < arr.Length)
                        value = arr.GetValue(row);
                }
                else
                {
                    var ie = dataSeries.Data as IEnumerable;
                    if (ie != null && !(dataSeries.Data is string)) // String is also IEnumerable
                    {
                        var ce = dataSeries.GetCachedEnumerable(ie);
                        if (row < ce.Length)
                            value = ce.GetValue(row);
                    }
                    else
                        value = dataSeries.Data;
                }

                // Apply converter if needed
                if (useConverters && value != null && dataSeries.Converter != null)
                    return dataSeries.Converter.Convert(value, typeof(object), dataSeries, null);
                else
                    return value;
            }
        }
        /// <summary>
        /// Returns the value of a specific property.
        /// </summary>
        /// <param name="i">The index of <see cref="DataSeries"/> associated with the property.</param>
        /// <returns>Value of a property with index <paramref name="i"/>.</returns>
        public object GetValue(int i)
        {
            // Get data series by inex
            var dataSeries = dataCollection[i];
            if (dataSeries.Data == null)
                if (dataSeries.Key == "X")
                    return row;
                else
                    return null;

            // Get value
            object value = null;
            var arr = dataSeries.Data as Array;
            if (arr != null)
            {
                if(row < arr.Length)
                    value = arr.GetValue(row);
            }
            else
            {
                var ie = dataSeries.Data as IEnumerable;
                if (ie != null && !(dataSeries.Data is string)) // String is also IEnumerable
                {
                    var ce = dataSeries.GetCachedEnumerable(ie);
                    if(row < ce.Length)
                        value = ce.GetValue(row);
                }
                else
                    value = dataSeries.Data;
            }

            // Apply converter if needed
            if (useConverters && value != null && dataSeries.Converter != null)
                return dataSeries.Converter.Convert(value, typeof(object), dataSeries, null);
            else
                return value;
        }

        /// <summary>
        /// Returns the value of a specific property without convertion.
        /// </summary>
        /// <param name="i">The index of data series associated with the property.</param>
        /// <returns>Value of a property with index <paramref name="i"/> without convertion.</returns>
        public object GetOriginalValue(int i)
        {
            return Sources.GetValue(i);
        }

        /// <summary>
        /// Gets the value of <see cref="DataSeries"/> with key "X".
        /// </summary>
        public object X
        {
            get
            {
                int i = dataCollection.XSeriesNumber;
                if (i == -1)
                    return row;
                else
                    return GetValue(i);
            }
        }

        /// <summary>
        /// Gets the value of <see cref="DataSeries"/> with key "X" without convertion.
        /// </summary>
        public object OriginalX
        {
            get
            {
                int i = dataCollection.XSeriesNumber;
                if (i == -1)
                    return row;
                else
                    return GetOriginalValue(i);
            }
        }

        /// <summary>
        /// Gets the stroke of a parent <see cref="MarkerGraph"/> of <see cref="DataCollection"/>.
        /// </summary>
        public SolidColorBrush Stroke
        {
            get
            {
                if (dataCollection.Count > 0)
                    if (dataCollection[0].Owner != null)
                        return dataCollection[0].Owner.Stroke;
                return null;
            }
        }

        /// <summary>
        /// Gets the stroke thickness of a parent <see cref="MarkerGraph"/> of <see cref="DataCollection"/>.
        /// </summary>
        public double StrokeThickness
        {
            get
            {
                if (dataCollection.Count > 0)
                    if (dataCollection[0].Owner != null)
                        return dataCollection[0].Owner.StrokeThickness;
                return 0;
            }
        }

        /// <summary>
        /// Gets current instance of <see cref="MarkerViewModel"/>.
        /// </summary>
        public MarkerViewModel This
        {
            get { return this; }
        }

        /// <summary>
        /// Event that occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        /// <summary>
        /// Notifies all bindings that all properies of <see cref="MarkerViewModel"/> have changed.
        /// </summary>
        public void Notify(string[] props)
        {
            if (props == null)
                NotifyPropertyChanged(null);
            else
            {
                foreach (var p in props)
                    NotifyPropertyChanged(p);
                if(props.Length > 0)
                    NotifyPropertyChanged("This");
            }
        }
    }
}

