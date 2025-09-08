using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// Список элементов с возможностью выбрать один из них.
/// </summary>
public class ListBox : Control
{
    /// <summary>
    /// Количество элементов, которое пропускается при обработке нажатий PgUp и PgDown.
    /// По умолчанию null, и нажатие PgUp и PgDown эквивалентно нажатию Home и End.
    /// </summary>
    public int? PageSize { get; set; }

    private readonly ObservableList<string> _items = new ObservableList<string>(new List<string>());

    public ObservableList<String> Items => _items;

    private int? _selectedItemIndex;

    public event EventHandler? SelectedItemIndexChanged;

    private readonly List<int> _disabledItemsIndexes = new List<int>();

    public List<int> DisabledItemsIndexes => _disabledItemsIndexes;

    public int? SelectedItemIndex
    {
        get => _selectedItemIndex;
        set
        {
            if (_selectedItemIndex != value)
            {
                _selectedItemIndex = value;
                if (null != SelectedItemIndexChanged)
                    SelectedItemIndexChanged(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    public ListBox()
    {
        Focusable = true;
        AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
        AddHandler(MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
        AddHandler(MouseMoveEvent, new MouseEventHandler(OnMouseMove));
        AddHandler(MouseWheelEvent, new MouseWheelEventHandler(OnMouseWheel));
        _items.ListChanged += (sender, args) =>
        {
            // Shift indexes of disabled items
            switch (args.Type)
            {
                case ListChangedEventType.ItemsInserted:
                {
                    List<int> disabledIndexesCopy = new List<int>(DisabledItemsIndexes);
                    foreach (int index in disabledIndexesCopy)
                    {
                        if (index >= args.Index)
                        {
                            DisabledItemsIndexes.Remove(index);
                            DisabledItemsIndexes.Add(index + args.Count);
                        }
                    }

                    // Selected item should stay the same after insertion
                    if (_selectedItemIndex.HasValue && _selectedItemIndex.Value >= args.Index)
                    {
                        SelectedItemIndex = Math.Min(_items.Count - 1, _selectedItemIndex.Value + args.Count);
                    }

                    break;
                }
                case ListChangedEventType.ItemsRemoved:
                {
                    List<int> disabledIndexesCopy = new List<int>(DisabledItemsIndexes);
                    foreach (int index in disabledIndexesCopy)
                    {
                        if (index >= args.Index)
                        {
                            DisabledItemsIndexes.Remove(index);
                            DisabledItemsIndexes.Add(index - args.Count);
                        }
                    }

                    // When removing, selectedItemIndex should be unchanged. If it is impossible
                    // (for example, if selectedItemIndex points to disabled item now) we should reset it to null
                    if (_selectedItemIndex.HasValue)
                    {
                        if (_selectedItemIndex >= _items.Count
                            || _disabledItemsIndexes.Contains(_selectedItemIndex.Value))
                        {
                            SelectedItemIndex = null;
                        }
                    }

                    break;
                }
                case ListChangedEventType.ItemReplaced:
                {
                    // Nothing to do
                    break;
                }
                default:
                    throw new NotSupportedException();
            }

            Invalidate();
        };
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs args)
    {
        if (args.Delta > 0)
        {
            PageUpCore(2);
        }
        else
        {
            PageDownCore(2);
        }

        args.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed)
        {
            int index = args.GetPosition(this).Y;
            if (!_disabledItemsIndexes.Contains(index) && SelectedItemIndex != index)
            {
                SelectedItemIndex = index;
                Invalidate();
            }
        }

        args.Handled = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        int index = args.GetPosition(this).Y;
        if (!_disabledItemsIndexes.Contains(index) && SelectedItemIndex != index)
        {
            SelectedItemIndex = index;
            Invalidate();
        }

        if (_disabledItemsIndexes.Contains(index))
            args.Handled = true;
    }

    private void PageUpCore(int? pageSize)
    {
        if (AllItemsAreDisabled) return;
        if (pageSize == null)
        {
            int firstEnabledIndex = 0;
            bool found = false;
            for (int i = 0; i < _items.Count && !found; i++)
            {
                if (!_disabledItemsIndexes.Contains(firstEnabledIndex)) found = true;
                else firstEnabledIndex = (firstEnabledIndex + 1) % _items.Count;
            }

            if (found && _selectedItemIndex != firstEnabledIndex)
            {
                SelectedItemIndex = firstEnabledIndex;
            }
        }
        else
        {
            if (SelectedItemIndex.HasValue && SelectedItemIndex != 0)
            {
                int newIndex = Math.Max(0, SelectedItemIndex.Value - pageSize.Value);

                // If it is disabled, take the first non-disabled item before
                while (_disabledItemsIndexes.Contains(newIndex)
                       && newIndex > 0)
                {
                    newIndex--;
                }

                if (!_disabledItemsIndexes.Contains(newIndex))
                {
                    SelectedItemIndex = newIndex;
                }
            }
        }

        CurrentItemShouldBeVisibleAtTop();
    }

    /// <summary>
    /// Makes the current selected item to be visible at bottom of scroll viewer
    /// (if any wrapping scroll viewer presents).
    /// Call this method after any PgDown or keyboard down arrow handling.
    /// </summary>
    private void CurrentItemShouldBeVisibleAtBottom()
    {
        var indexValue = SelectedItemIndex ?? 0;
        // Notify any ScrollViewer that wraps this control to scroll visible part
        RaiseEvent(ScrollViewer.ContentShouldBeScrolledEvent,
            new ContentShouldBeScrolledEventArgs(this,
                ScrollViewer.ContentShouldBeScrolledEvent,
                null, null, null,
                Math.Max(0, indexValue)));
    }

    /// <summary>
    /// Makes the current selected item to be visible at top of scroll viewer
    /// (if any wrapping scroll viewer presents).
    /// Call this method after any PgUp or keyboard up arrow handling.
    /// </summary>
    private void CurrentItemShouldBeVisibleAtTop()
    {
        var indexValue = SelectedItemIndex ?? 0;
        // Notify any ScrollViewer that wraps this control to scroll visible part
        RaiseEvent(ScrollViewer.ContentShouldBeScrolledEvent,
            new ContentShouldBeScrolledEventArgs(this,
                ScrollViewer.ContentShouldBeScrolledEvent,
                null, null,
                Math.Max(0, indexValue),
                null));
    }

    private void PageDownCore(int? pageSize)
    {
        if (AllItemsAreDisabled) return;
        int itemIndex = SelectedItemIndex ?? 0;
        if (pageSize == null)
        {
            if (!AllItemsAreDisabled && itemIndex != _items.Count - 1)
            {
                // Take the last non-disabled item
                int firstEnabledItemIndex = _items.Count - 1;
                while (_disabledItemsIndexes.Contains(firstEnabledItemIndex))
                {
                    firstEnabledItemIndex--;
                }

                SelectedItemIndex = firstEnabledItemIndex;
            }
        }
        else
        {
            if (itemIndex != _items.Count - 1)
            {
                int newIndex = Math.Min(_items.Count - 1, itemIndex + pageSize.Value);

                // If it is disabled, take the first non-disabled item after
                while (_disabledItemsIndexes.Contains(newIndex)
                       && newIndex < _items.Count - 1)
                {
                    newIndex++;
                }

                if (!_disabledItemsIndexes.Contains(newIndex))
                {
                    SelectedItemIndex = newIndex;
                }
            }
        }

        CurrentItemShouldBeVisibleAtBottom();
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (_items.Count == 0)
        {
            args.Handled = true;
            return;
        }

        if (args.wVirtualKeyCode == VirtualKeys.PageUp)
        {
            PageUpCore(PageSize);
        }

        if (args.wVirtualKeyCode == VirtualKeys.PageDown)
        {
            PageDownCore(PageSize);
        }

        if (args.wVirtualKeyCode == VirtualKeys.Up)
        {
            if (AllItemsAreDisabled) return;
            do
            {
                if (SelectedItemIndex == 0 || SelectedItemIndex == null)
                    SelectedItemIndex = _items.Count - 1;
                else
                {
                    SelectedItemIndex--;
                }
            } while (_disabledItemsIndexes.Contains(SelectedItemIndex.Value));

            CurrentItemShouldBeVisibleAtTop();
        }

        if (args.wVirtualKeyCode == VirtualKeys.Down)
        {
            if (AllItemsAreDisabled) return;
            do
            {
                if (SelectedItemIndex == null) SelectedItemIndex = 0;
                SelectedItemIndex = (SelectedItemIndex + 1) % _items.Count;
            } while (_disabledItemsIndexes.Contains(SelectedItemIndex.Value));

            CurrentItemShouldBeVisibleAtBottom();
        }

        args.Handled = true;
    }

    private bool AllItemsAreDisabled => _disabledItemsIndexes.Count == _items.Count;

    protected override Size MeasureOverride(Size availableSize)
    {
        // если maxLen < availableSize.Width, то возвращается maxLen
        // если maxLen > availableSize.Width, возвращаем availableSize.Width,
        // а содержимое не влезающих строк будет выведено с многоточием
        if (_items.Count == 0) return new Size(0, 0);
        int maxLen = _items.Max(s => s.Length);
        // 1 пиксель слева и 1 справа
        Size size = new Size(Math.Min(maxLen + 2, availableSize.Width), _items.Count);
        return size;
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr selectedAttr = Colors.Blend(Color.White, Color.DarkGreen);
        Attr attr = Colors.Blend(Color.Black, Color.DarkCyan);
        Attr disabledAttr = Colors.Blend(Color.Gray, Color.DarkCyan);
        for (int y = 0; y < ActualHeight; y++)
        {
            string? item = y < _items.Count ? _items[y] : null;

            if (item != null)
            {
                Attr currentAttr = _disabledItemsIndexes.Contains(y)
                    ? disabledAttr
                    : (SelectedItemIndex == y ? selectedAttr : attr);

                buffer.SetPixel(0, y, ' ', currentAttr);
                if (ActualWidth > 1)
                {
                    // минус 2 потому что у нас есть по пустому пикселю слева и справа
                    int rendered = RenderString(item, buffer, 1, y, ActualWidth - 2, currentAttr);
                    buffer.FillRectangle(1 + rendered, y, ActualWidth - (1 + rendered), 1, ' ',
                        currentAttr);
                }
            }
            else
            {
                buffer.FillRectangle(0, y, ActualWidth, 1, ' ', attr);
            }
        }
    }
}