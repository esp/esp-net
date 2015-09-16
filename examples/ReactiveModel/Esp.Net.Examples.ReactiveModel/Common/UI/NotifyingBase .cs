using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Esp.Net.Examples.ReactiveModel.Common.UI
{
    public class ViewModelBase : DisposableBase, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected bool SetProperty<T>(ref T backingField, T newValue, [CallerMemberName] string propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(backingField, newValue))
            {
                backingField = newValue;
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }
    }
}