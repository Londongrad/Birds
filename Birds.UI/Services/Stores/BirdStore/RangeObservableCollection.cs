using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Birds.UI.Services.Stores.BirdStore
{
    public sealed class RangeObservableCollection<T> : ObservableCollection<T>
    {
        public void ReplaceAll(IEnumerable<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            CheckReentrancy();

            Items.Clear();

            foreach (var item in items)
                Items.Add(item);

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
