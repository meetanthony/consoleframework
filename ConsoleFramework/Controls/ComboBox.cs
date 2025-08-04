using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// В свёрнутом состоянии представляет собой однострочный контрол. При разворачивании списка
/// создаётся всплывающее модальное кастомное окошко и показывается пользователю, причём первая
/// строчка этого окна - прозрачная и через неё видно сам комбобокс (это нужно для того, чтобы
/// обрабатывать клики по комбобоксу - при клике на прозрачную область комбобокс должен сворачиваться).
/// Если этого бы не было, то с учётом того, что модальное окно показывается с флагом
/// outsideClickWillCloseWindow = true, клик по самому комбобоксу приводил бы к мгновенному закрытию
/// и открытию комбобокса заново.
/// </summary>
public class ComboBox : Control
{
    private readonly bool _shadow;

    public ComboBox() : this(true)
    {
    }

    /// <summary>
    /// Creates combobox instance.
    /// </summary>
    /// <param name="shadow">Display shadow or not</param>
    public ComboBox(bool shadow)
    {
        _shadow = shadow;
        Focusable = true;
        AddHandler(MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
        AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
    }

    private class PopupWindow : Window
    {
        public int? IndexSelected;
        private readonly bool _shadow;
        private readonly ListBox _listbox;
        private readonly ScrollViewer _scrollViewer;

        public PopupWindow(IEnumerable<string> items,
            int selectedItemIndex, bool shadow,
            int? shownItemsCount)
        {
            _shadow = shadow;
            _scrollViewer = new ScrollViewer();
            _listbox = new ListBox();
            foreach (string item in items) _listbox.Items.Add(item);
//                listbox.Items.AddRange( items );
            _listbox.SelectedItemIndex = selectedItemIndex;
            if (shownItemsCount != null)
                _listbox.PageSize = shownItemsCount.Value;
            IndexSelected = selectedItemIndex;
            _listbox.HorizontalAlignment = HorizontalAlignment.Stretch;
            _scrollViewer.HorizontalScrollEnabled = false;
            _scrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
            _scrollViewer.Content = _listbox;
            Content = _scrollViewer;

            // If click on the transparent header, close the popup
            AddHandler(MouseDownEvent, new MouseButtonEventHandler((sender, args) =>
            {
                if (!_scrollViewer.RenderSlotRect.Contains(args.GetPosition(this)))
                {
                    Close();
                    args.Handled = true;
                }
            }));

            // If listbox item has been selected
            EventManager.AddHandler(_listbox, MouseUpEvent, new MouseButtonEventHandler((sender, args) =>
            {
                IndexSelected = _listbox.SelectedItemIndex;
                Close();
            }), true);
            EventManager.AddHandler(_listbox, KeyDownEvent, new KeyEventHandler((sender, args) =>
            {
                if (args.wVirtualKeyCode == VirtualKeys.Return)
                {
                    IndexSelected = _listbox.SelectedItemIndex;
                    Close();
                }
            }), true);
            // todo : cleanup event handlers after popup closing
        }

        private void InitListBoxScrollingPos()
        {
            int itemIndex = _listbox.SelectedItemIndex ?? 0;
            int firstVisibleItemIndex = _scrollViewer.DeltaY;
            int lastVisibleItemIndex = firstVisibleItemIndex + _scrollViewer.ActualHeight -
                                       (_scrollViewer.HorizontalScrollVisible ? 1 : 0) - 1;
            if (itemIndex > lastVisibleItemIndex)
            {
                _scrollViewer.ScrollContent(ScrollViewer.Direction.Up, itemIndex - lastVisibleItemIndex);
            }
            else if (itemIndex < firstVisibleItemIndex)
            {
                _scrollViewer.ScrollContent(ScrollViewer.Direction.Down, firstVisibleItemIndex - itemIndex);
            }
        }

        protected override void Initialize()
        {
            AddHandler(ActivatedEvent, new EventHandler(OnActivated));
            AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown), true);
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (args.wVirtualKeyCode == VirtualKeys.Escape)
            {
                Close();
            }
            else base.OnPreviewKeyDown(sender, args);
        }

        private void OnActivated(object? sender, EventArgs eventArgs)
        {
        }

        public override void Render(RenderingBuffer buffer)
        {
            Attr borderAttrs = Colors.Blend(Color.Black, Color.DarkCyan);

            // Background
            buffer.FillRectangle(1, 1, ActualWidth - 1, ActualHeight - 1, ' ', borderAttrs);

            // First row and first column are transparent
            // Column is also transparent for mouse events
            buffer.SetOpacityRect(0, 0, ActualWidth, 1, 2);
            buffer.SetOpacityRect(0, 1, 1, ActualHeight - 1, 6);
            if (_shadow)
            {
                buffer.SetOpacity(1, ActualHeight - 1, 2 + 4);
                buffer.SetOpacity(ActualWidth - 1, 0, 2 + 4);
                buffer.SetOpacityRect(ActualWidth - 1, 1, 1, ActualHeight - 1, 1 + 4);
                buffer.FillRectangle(ActualWidth - 1, 1, 1, ActualHeight - 1, UnicodeTable.FullBlock, borderAttrs);
                buffer.SetOpacityRect(2, ActualHeight - 1, ActualWidth - 2, 1, 3 + 4);
                buffer.FillRectangle(2, ActualHeight - 1, ActualWidth - 2, 1, UnicodeTable.UpperHalfBlock,
                    Attr.NO_ATTRIBUTES);
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Content == null) return new Size(0, 0);
            if (_shadow)
            {
                // 1 row and 1 column - reserved for transparent space, remaining - for ListBox
                Content.Measure(new Size(availableSize.Width - 2, availableSize.Height - 2));
                return new Size(Content.DesiredSize.Width + 2, Content.DesiredSize.Height + 2);
            }
            else
            {
                // 1 row and 1 column - reserved for transparent space, remaining - for ListBox
                Content.Measure(new Size(availableSize.Width - 1, availableSize.Height - 1));
                return new Size(Content.DesiredSize.Width + 1, Content.DesiredSize.Height + 1);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Content != null)
            {
                if (_shadow)
                {
                    Content.Arrange(new Rect(new Point(1, 1),
                        new Size(finalSize.Width - 2, finalSize.Height - 2)));
                }
                else
                {
                    Content.Arrange(new Rect(new Point(1, 1),
                        new Size(finalSize.Width - 1, finalSize.Height - 1)));
                }

                // When initializing we need to correctly assign offsets to ScrollViewer for
                // currently selected item. Because ScrollViewer depends of ActualWidth / ActualHeight
                // of Content, we need to do this after arrangement has finished.
                InitListBoxScrollingPos();
            }

            return finalSize;
        }

        public override string ToString()
        {
            return "ComboBox.PopupWindow";
        }
    }

    private bool Opened
    {
        get => _mOpened;
        set
        {
            _mOpened = value;
            Invalidate();
        }
    }

    public int? ShownItemsCount { get; set; }

    private void OpenPopup()
    {
        if (Opened) throw new InvalidOperationException("Assertion failed.");
        Window popup = new PopupWindow(Items, SelectedItemIndex ?? 0, _shadow,
            ShownItemsCount != null ? ShownItemsCount.Value - 1 : null);
        Point popupCoord = TranslatePoint(this, new Point(0, 0),
            VisualTreeHelper.FindClosestParent<WindowsHost>(this));
        popup.X = popupCoord.X;
        popup.Y = popupCoord.Y;
        popup.Width = _shadow ? ActualWidth + 1 : ActualWidth;
        if (Items.Count != 0)
            popup.Height = (ShownItemsCount != null ? ShownItemsCount.Value : Items.Count)
                           + (_shadow ? 2 : 1); // 1 row for transparent "header"
        else popup.Height = _shadow ? 3 : 2;
        WindowsHost? windowsHost = VisualTreeHelper.FindClosestParent<WindowsHost>(this);
        windowsHost?.ShowModal(popup, true);
        Opened = true;
        EventManager.AddHandler(popup, Window.ClosedEvent, new EventHandler(OnPopupClosed));
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.wVirtualKeyCode == VirtualKeys.Return)
        {
            OpenPopup();
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs mouseButtonEventArgs)
    {
        if (!Opened)
            OpenPopup();
    }

    private void OnPopupClosed(object? o, EventArgs args)
    {
        if (!Opened) throw new InvalidOperationException("Assertion failed.");
        Opened = false;
        if (o != null)
        {
            SelectedItemIndex = ((PopupWindow)o).IndexSelected;
            EventManager.RemoveHandler(o, Window.ClosedEvent, new EventHandler(OnPopupClosed));
        }
    }

    private readonly List<String> _items = new List<string>();

    public List<String> Items => _items;

    public int? SelectedItemIndex
    {
        get => _selectedItemIndex;
        set
        {
            if (_selectedItemIndex != value)
            {
                _selectedItemIndex = value;
                Invalidate();
                RaisePropertyChanged("SelectedItemIndex");
            }
        }
    }

    private bool _mOpened;
    private int? _selectedItemIndex;

    public static Size EmptySize = new Size(3, 1);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Items.Count == 0) return EmptySize;
        int maxLen = Items.Max(s => s.Length);
        // 1 pixel from left, 1 from right, then arrow and 1 more empty pixel
        Size size = new Size(Math.Min(maxLen + 4, availableSize.Width), 1);
        return size;
    }

    public override void Render(RenderingBuffer buffer)
    {
        var attrs = HasFocus
            ? Colors.Blend(Color.White, Color.DarkGreen)
            : Colors.Blend(Color.Black, Color.DarkCyan);

        buffer.SetPixel(0, 0, ' ', attrs);
        int usedForCurrentItem = 0;
        if (Items.Count != 0 && ActualWidth > 4)
        {
            usedForCurrentItem = RenderString(Items[SelectedItemIndex ?? 0], buffer, 1, 0, ActualWidth - 4, attrs);
        }

        buffer.FillRectangle(1 + usedForCurrentItem, 0, ActualWidth - (usedForCurrentItem + 1), 1, ' ', attrs);
        if (ActualWidth > 2)
        {
            buffer.SetPixel(ActualWidth - 2, 0, Opened ? '^' : 'v', attrs);
        }
    }
}