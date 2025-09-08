using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ConsoleFramework.Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Xaml;
using ListChangedEventArgs = ConsoleFramework.Binding.Observables.ListChangedEventArgs;

namespace ConsoleFramework.Controls;

public interface IItemsSource
{
    IList<TreeItem> GetItems();
}

[ContentProperty(nameof(Items))]
public class TreeItem : INotifyPropertyChanged
{
    /// <summary>
    /// Pos in TreeView listbox.
    /// </summary>
    internal int Position;

    internal int Level;

    internal String DisplayTitle
    {
        get
        {
            if (Items.Count != 0)
                return string.Format("{0}{1} {2}", new string(' ', Level * 2),
                    (Expanded ? UnicodeTable.ArrowDown : UnicodeTable.ArrowRight), Title);
            return $"{new string(' ', (Level + 1) * 2)}{Title}";
        }
    }

    // todo : call listBox.Invalidate() if item is visible now
    private string _title = String.Empty;

    public String Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                RaisePropertyChanged("Title");
                RaisePropertyChanged("DisplayTitle");
            }
        }
    }

    private bool _disabled;

    public bool Disabled
    {
        get => _disabled;
        set
        {
            if (_disabled != value)
            {
                _disabled = value;
                RaisePropertyChanged("Disabled");
            }
        }
    }

    internal readonly ObservableList<TreeItem> ItemsInternal = new ObservableList<TreeItem>(new List<TreeItem>());

    public IList<TreeItem> Items => ItemsInternal;

    public bool HasChildren => ItemsInternal.Count != 0;

    public IItemsSource? ItemsSource { get; set; }

    internal bool ExpandedInternal;

    public bool Expanded
    {
        get => ExpandedInternal;
        set
        {
            if (ExpandedInternal != value)
            {
                ExpandedInternal = value;
                RaisePropertyChanged("Expanded");
                RaisePropertyChanged("DisplayTitle");
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void RaisePropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler? handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }
}

[ContentProperty(nameof(Items))]
public class TreeView : Control
{
    private readonly ObservableList<TreeItem> _items = new ObservableList<TreeItem>(
        new List<TreeItem>());

    public IList<TreeItem> Items => _items;

    public IItemsSource? ItemsSource { get; set; }

    private readonly ListBox _listBox;

    public TreeItem? SelectedItem
    {
        get
        {
            if (_treeItemsFlat.Count == 0) return null;
            if (_listBox.SelectedItemIndex == null) return null;
            return _treeItemsFlat[_listBox.SelectedItemIndex.Value];
        }
    }

    public TreeView()
    {
        _listBox = new ListBox();
        _listBox.HorizontalAlignment = HorizontalAlignment.Stretch;
        _listBox.VerticalAlignment = VerticalAlignment.Stretch;

        // Stretch by default too
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;

        AddChild(_listBox);
        _items.ListChanged += ItemsOnListChanged;

        _listBox.AddHandler(MouseDownEvent, new MouseEventHandler((sender, args) =>
        {
            if (!args.Handled)
            {
                if (_listBox.SelectedItemIndex.HasValue)
                    ExpandCollapse(_treeItemsFlat[_listBox.SelectedItemIndex.Value]);
            }
        }), true);

        _listBox.SelectedItemIndexChanged += (sender, args) => { RaisePropertyChanged("SelectedItem"); };
    }

    private void SubscribeToItem(TreeItem item, ListChangedHandler handler)
    {
        item.ItemsInternal.ListChanged += handler;
        item.PropertyChanged += ItemOnPropertyChanged;
        foreach (TreeItem child in item.ItemsInternal)
        {
            SubscribeToItem(child, handler);
        }
    }

    private void UnsubscribeFromItem(TreeItem item, ListChangedHandler handler)
    {
        item.ItemsInternal.ListChanged -= handler;
        item.PropertyChanged -= ItemOnPropertyChanged;
        foreach (TreeItem child in item.ItemsInternal)
        {
            UnsubscribeFromItem(child, handler);
        }
    }

    private void ItemOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        TreeItem? senderItem = sender as TreeItem;
        
        if (senderItem == null)
            return;
        
        if (args.PropertyName == "DisplayTitle")
        {
            if (senderItem.Position >= 0)
            {
                _listBox.Items[senderItem.Position] = senderItem.DisplayTitle;
            }
        }

        if (args.PropertyName == "Disabled")
        {
            if (senderItem.Position >= 0)
            {
                if (senderItem.Disabled)
                    _listBox.DisabledItemsIndexes.Add(senderItem.Position);
                else
                    _listBox.DisabledItemsIndexes.Remove(senderItem.Position);
            }
        }

        if (args.PropertyName == "Expanded")
        {
            if (senderItem.Position >= 0)
            {
                if (senderItem.Expanded)
                    Expand(senderItem);
                else
                    Collapse(senderItem);
            }
        }
    }

    private void EnsureFlatListIsCorrect()
    {
        for (int i = 0; i < _treeItemsFlat.Count; i++)
        {
            Assert(_treeItemsFlat[i].Position == i);
        }
    }

    /// <summary>
    /// Maintains the correct order of items in flat list.
    /// </summary>
    private void OnItemInserted(int pos)
    {
        TreeItem treeItem = _items[pos];
        TreeItem? prevItem = null;
        if (pos > 0)
            prevItem = _items[pos];
        treeItem.Position = prevItem != null ? prevItem.Position + 1 : _items.Count - 1;
        for (int j = treeItem.Position; j < _treeItemsFlat.Count; j++)
        {
            _treeItemsFlat[j].Position++;
        }

        _treeItemsFlat.Insert(treeItem.Position, treeItem);
        _listBox.Items.Insert(treeItem.Position, treeItem.DisplayTitle);
        if (treeItem.Disabled)
            _listBox.DisabledItemsIndexes.Add(treeItem.Position);

        // Handle modification of inner list recursively
        SubscribeToItem(treeItem, ItemsOnListChanged);
        if (treeItem.Position <= _listBox.SelectedItemIndex)
            RaisePropertyChanged("SelectedItem");

        EnsureFlatListIsCorrect();
    }

    private void OnItemRemoved(TreeItem treeItem)
    {
        if (treeItem.Expanded) Collapse(treeItem);
        _treeItemsFlat.RemoveAt(treeItem.Position);
        _listBox.Items.RemoveAt(treeItem.Position);
        for (int j = treeItem.Position; j < _treeItemsFlat.Count; j++)
            _treeItemsFlat[j].Position--;

        // Cleanup event handler recursively
        UnsubscribeFromItem(treeItem, ItemsOnListChanged);

        if (_listBox.SelectedItemIndex >= treeItem.Position)
            RaisePropertyChanged("SelectedItem");

        EnsureFlatListIsCorrect();
    }

    private void ItemsOnListChanged(object sender, ListChangedEventArgs args)
    {
        var removedItems = args.RemovedItems;
        
        switch (args.Type)
        {
            case ListChangedEventType.ItemsInserted:
            {
                for (int i = 0; i < args.Count; i++)
                    OnItemInserted(i + args.Index);
                break;
            }
            case ListChangedEventType.ItemsRemoved:
            {
                if (removedItems != null)
                    foreach (TreeItem treeItem in removedItems.Cast<TreeItem>())
                        OnItemRemoved(treeItem);
                break;
            }
            case ListChangedEventType.ItemReplaced:
            {
                if (removedItems is { Count: > 0 } && removedItems[0] is TreeItem removedItem)
                {
                    OnItemRemoved(removedItem);
                }
                OnItemInserted(args.Index);
                break;
            }
        }
    }

    /// <summary>
    /// Flat list of tree items in order corresponding to actual listbox content.
    /// </summary>
    private readonly List<TreeItem> _treeItemsFlat = new List<TreeItem>();

    private void Expand(TreeItem item)
    {
        int index = _treeItemsFlat.IndexOf(item);
        for (int i = 0; i < item.Items.Count; i++)
        {
            TreeItem child = item.Items[i];
            _treeItemsFlat.Insert(i + index + 1, child);
            child.Position = i + index + 1;
            child.Level = item.Level + 1;

            // Учесть уровень вложенности в title
            _listBox.Items.Insert(i + index + 1, child.DisplayTitle);
            if (child.Disabled) _listBox.DisabledItemsIndexes.Add(i + index + 1);
        }

        for (int k = index + 1 + item.Items.Count; k < _treeItemsFlat.Count; k++)
        {
            _treeItemsFlat[k].Position += item.Items.Count;
        }

        // Children are expanded too according to their Expanded stored state
        foreach (TreeItem child in item.Items.Where(child => child.Expanded))
        {
            Expand(child);
        }

        EnsureFlatListIsCorrect();
    }

    private void Collapse(TreeItem item)
    {
        // Children are collapsed but with Expanded state saved
        foreach (TreeItem child in item.Items.Where(child => child.Expanded))
        {
            Collapse(child);
        }

        int index = _treeItemsFlat.IndexOf(item);
        foreach (TreeItem child in item.Items)
        {
            _treeItemsFlat.RemoveAt(index + 1);
            if (child.Disabled) _listBox.DisabledItemsIndexes.Remove(index + 1);
            _listBox.Items.RemoveAt(index + 1);
            child.Position = -1;
        }

        for (int k = index + 1; k < _treeItemsFlat.Count; k++)
        {
            _treeItemsFlat[k].Position -= item.Items.Count;
        }

        EnsureFlatListIsCorrect();
    }

    private void ExpandCollapse(TreeItem item)
    {
        int index = _treeItemsFlat.IndexOf(item);
        if (item.Expanded)
        {
            Collapse(item);
            item.ExpandedInternal = false;
            // Need to update item string (because Expanded status has been changed)
            _listBox.Items[index] = item.DisplayTitle;
        }
        else
        {
            Expand(item);
            item.ExpandedInternal = true;
            // Need to update item string (because Expanded status has been changed)
            _listBox.Items[index] = item.DisplayTitle;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _listBox.Measure(availableSize);
        return _listBox.DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _listBox.Arrange(new Rect(finalSize));
        return finalSize;
    }
}