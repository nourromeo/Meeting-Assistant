using System.Collections.ObjectModel;

namespace XlentWPF.ViewModels;

public class ObservableCollectionEx<T> : ObservableCollection<T>
{
    public new T this[int index]
    {
        get => base[index];
        set => SetItem(index, value);
    }
}
