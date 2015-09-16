using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Esp.Net.Examples.ReactiveModel.Common.Model.Entities.Fields
{
    public interface ISelectionField<out T> : IField<T>
    {
        IReadOnlyCollection<T> Items { get; }
    }

    public class SelectionField<T> : Field<T>, ISelectionField<T>
    {
        private readonly List<T> _items;
        private readonly ReadOnlyCollection<T> _readOnlyItems;

        public SelectionField()
        {
            _items = new List<T>();
            _readOnlyItems = new ReadOnlyCollection<T>(_items);
        }

        public List<T> Items
        {
            get { return _items; }
        }

        public void ResetItems(IEnumerable<T> items)
        {
            _items.Clear();
            _items.AddRange(items);
        }

        IReadOnlyCollection<T> ISelectionField<T>.Items
        {
            get
            {
                return _readOnlyItems;
            }
        }
    }
}