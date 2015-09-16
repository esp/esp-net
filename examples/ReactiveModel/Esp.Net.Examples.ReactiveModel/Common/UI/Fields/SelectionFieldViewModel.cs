using System.Collections.ObjectModel;
using System.Linq;
using Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields;

namespace Esp.Net.Examples.ReactiveModel.Common.UI.Fields
{
    public class SelectionFieldViewModel<T> : FieldViewModel<T>
    {
        private readonly ObservableCollection<T> _items;

        public SelectionFieldViewModel()
        {
            _items = new ObservableCollection<T>();
            Items = new ReadOnlyObservableCollection<T>(_items);
        }

        public ReadOnlyObservableCollection<T> Items { get; private set; }

        public void Sync(ISelectionField<T> model)
        {
            base.Sync(model);

            // Note: just using a bruit force clear->repopulate approach here. 
            // In reality you'd build a deferring ObservableCollection
            // that supports 'AddRange' (i.e. doesn't raise events for bulk updates) and doesn't trash all the items.
            // You don't want to trash all the items as something on the UI may be binding to them, 
            // you'll suffer a performance hit with the below approach. 
            // It's fine for a demo
            if (!model.Items.SequenceEqual(Items))
            {
                _items.Clear();
                foreach (T item in model.Items)
                {
                    _items.Add(item);
                }
            }
        }
    }
}