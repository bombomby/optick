using System;
using System.Collections.Generic;
using System.ComponentModel;                      //INotifyPropertyChanged
using System.Runtime.CompilerServices;            //CallerMemberName
// using System.Windows;                             //DependencyObject, DependencyProperty            

namespace Profiler.InfrastructureMvvm
{
    public abstract class BaseViewModel : INotifyPropertyChanged // DependencyObject, 
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value,[CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
