using System;
using System.Collections.Generic;

namespace ConsoleFramework.Binding.Observables;

/// <summary>
/// Marks the IList or IList&lt;T&gt; with notifications support.
/// It is not derived from IList and IList&lt;T&gt; to allow
/// to create both generic and nongeneric implementations.
/// </summary>
public interface IObservableList
{
    event ListChangedHandler ListChanged;
}

public delegate void ListChangedHandler(object sender, ListChangedEventArgs args);

public enum ListChangedEventType
{
    ItemsInserted,
    ItemsRemoved,
    ItemReplaced
}

public class ListChangedEventArgs : EventArgs
{
    public ListChangedEventArgs(ListChangedEventType type, int index, int count, List<Object?>? removedItems)
    {
        Type = type;
        Index = index;
        Count = count;
        RemovedItems = removedItems;
    }

    public readonly ListChangedEventType Type;
    public readonly int Index;
    public readonly int Count;
    public readonly List<Object?>? RemovedItems;
}