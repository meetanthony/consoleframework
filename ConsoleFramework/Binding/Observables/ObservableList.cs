using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleFramework.Binding.Observables;

/// <summary>
/// Non-generic <see cref="IObservableList"/> implementation.
/// </summary>
public class ObservableList : IObservableList, IList
{
    private readonly IList _list;

    public ObservableList(IList list)
    {
        _list = list;
    }

    public IEnumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Add(Object? item)
    {
        int index = _list.Count;
        _list.Add(item);

        RaiseListElementsAdded(index, 1);
        return index;
    }

    public void Clear()
    {
        int count = _list.Count;
        List<object?> removedItems = new();
        foreach (object item in _list)
        {
            removedItems.Add(item);
        }

        _list.Clear();

        RaiseListElementsRemoved(0, count, removedItems);
    }

    public bool Contains(Object? item)
    {
        return _list.Contains(item);
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public void Remove(Object? item)
    {
        int index = _list.IndexOf(item);
        _list.Remove(item);
        if (-1 != index)
            RaiseListElementsRemoved(index, 1, new() { item });
    }

    public void CopyTo(Array array, int index)
    {
        _list.CopyTo(array, index);
    }

    public int Count => _list.Count;

    public object SyncRoot => _list.SyncRoot;

    public bool IsSynchronized => _list.IsSynchronized;

    public bool IsReadOnly => _list.IsReadOnly;

    public bool IsFixedSize => _list.IsFixedSize;

    public int IndexOf(Object? item)
    {
        return _list.IndexOf(item);
    }

    public void Insert(int index, Object? item)
    {
        _list.Insert(index, item);
        RaiseListElementsAdded(index, 1);
    }

    public void RemoveAt(int index)
    {
        object? removedItem = _list[index];
        _list.RemoveAt(index);
        RaiseListElementsRemoved(index, 1, new() { removedItem });
    }

    public Object? this[int index]
    {
        get => _list[index];
        set
        {
            object? removedItem = _list[index];
            _list[index] = value;
            RaiseListElementReplaced(index, new() { removedItem });
        }
    }

    private void RaiseListElementsAdded(int index, int length)
    {
        if (null != ListChanged)
        {
            ListChanged.Invoke(this, new ListChangedEventArgs(ListChangedEventType.ItemsInserted, index, length, null));
        }
    }

    private void RaiseListElementsRemoved(int index, int length, List<object?>? removedItems)
    {
        if (null != ListChanged)
        {
            ListChanged.Invoke(this,
                new ListChangedEventArgs(ListChangedEventType.ItemsRemoved, index, length, removedItems));
        }
    }

    private void RaiseListElementReplaced(int index, List<object?> removedItems)
    {
        if (null != ListChanged)
        {
            ListChanged.Invoke(this,
                new ListChangedEventArgs(ListChangedEventType.ItemReplaced, index, 1, removedItems));
        }
    }

    public event ListChangedHandler? ListChanged;
}