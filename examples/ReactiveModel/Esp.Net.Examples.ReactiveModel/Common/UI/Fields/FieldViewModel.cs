using Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields;

namespace Esp.Net.Examples.ReactiveModel.Common.UI.Fields
{
    public class FieldViewModel<T> : ViewModelBase
    {
        private T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                SetProperty(ref _value, value);
            }
        }

        private bool _isValid;
        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                SetProperty(ref _isValid, value);
            }
        }

        private bool _hasValue;
        public bool HasValue
        {
            get { return _hasValue; }
            set
            {
                SetProperty(ref _hasValue, value);
            }
        }

        private bool _isEnabled;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                SetProperty(ref _isEnabled, value);
            }
        }

        public void Sync(IField<T> model)
        {
            Value = model.Value;
            IsValid = model.IsValid;
            IsEnabled = model.IsEnabled;
            HasValue = model.HasValue;
        }
    }
}