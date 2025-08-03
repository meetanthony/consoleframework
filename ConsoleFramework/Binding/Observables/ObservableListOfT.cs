using System;
using System.Collections;
using System.Collections.Generic;

namespace ConsoleFramework.Binding.Observables;

/// <summary>
/// Generic implementation of <see cref="IObservableList"/>.
/// Non-generic IList is implemented to enforce compatibility with
/// Collection&lt;T&gt; and List&lt;T&gt;.
/// </summary>
/// <typeparam name="T"></typeparam>
public class ObservableList<T> : IObservableList, IList<T>, IList
{
    private readonly IList<T> _list;

    public ObservableList(IList<T> list)
    {
        _list = list;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        int index = _list.Count;
        _list.Add(item);
        RaiseListElementsAdded(index, 1);
    }

    int IList.Add(object? value)
    {
        if (value == null)
            throw new NullReferenceException();
        int count = Count;
        Add((T)value);
        return count;
    }

    bool IList.Contains(object? value)
    {
        if (value == null)
            throw new NullReferenceException();
        return Contains((T)value);
    }

    public void Clear()
    {
        int count = _list.Count;
        List<object?> removedItems = new ();
        foreach (T item in _list)
        {
            removedItems.Add(item);
        }

        _list.Clear();

        RaiseListElementsRemoved(0, count, removedItems);
    }

    int IList.IndexOf(object? value)
    {
        if (value == null)
            throw new NullReferenceException();
        return IndexOf((T)value);
    }

    void IList.Insert(int index, object? value)
    {
        if (value == null)
            throw new NullReferenceException();
        Insert(index, (T)value);
    }

    void IList.Remove(object? value)
    {
        if (value == null)
            throw new NullReferenceException();
        Remove((T)value);
    }

    public bool Contains(T item)
    {
        return _list.Contains(item);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _list.CopyTo(array, arrayIndex);
    }

    public bool Remove(T item)
    {
        int index = _list.IndexOf(item);
        _list.Remove(item);
        if (-1 != index)
        {
            RaiseListElementsRemoved(index, 1, [item]);
            return true;
        }

        return false;
    }

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)_list).CopyTo(array, index);
    }

    public int Count => _list.Count;

    object ICollection.SyncRoot => ((ICollection)_list).SyncRoot;

    bool ICollection.IsSynchronized => ((ICollection)_list).IsSynchronized;

    public bool IsReadOnly => _list.IsReadOnly;

    bool IList.IsFixedSize => ((IList)_list).IsFixedSize;

    public int IndexOf(T item)
    {
        return _list.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        _list.Insert(index, item);
        RaiseListElementsAdded(index, 1);
    }

    public void RemoveAt(int index)
    {
        T removedItem = _list[index];
        _list.RemoveAt(index);
        RaiseListElementsRemoved(index, 1, [removedItem]);
    }

    object? IList.this[int index]
    {
        get => this[index];
        set => this[index] = (T)value;
    }

    public T this[int index]
    {
        get => _list[index];
        set
        {
            T removedItem = _list[index];
            _list[index] = value;
            RaiseListElementReplaced(index, [removedItem]);
        }
    }

    private void RaiseListElementsAdded(int index, int length)
    {
        OnListChanged(new ListChangedEventArgs(ListChangedEventType.ItemsInserted, index, length, null));
    }

    private void RaiseListElementsRemoved(int index, int length, List<object?> removedItems)
    {
        OnListChanged(new ListChangedEventArgs(ListChangedEventType.ItemsRemoved, index, length, [removedItems]));
    }

    private void RaiseListElementReplaced(int index, List<object?> removedItems)
    {
        OnListChanged(new ListChangedEventArgs(ListChangedEventType.ItemReplaced, index, 1, [removedItems]));
    }

    public event ListChangedHandler? ListChanged;

    protected virtual void OnListChanged(ListChangedEventArgs args)
    {
        ListChanged?.Invoke(this, args);
    }
}