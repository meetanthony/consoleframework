using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.Controls;

public enum MenuItemType
{
    Item,
    RootSubmenu,
    Submenu,
    Separator
}

public class MenuItemBase : Control;

/// <summary>
/// Item of menu.
/// </summary>
[ContentProperty(nameof(Items))]
public class MenuItem : MenuItemBase, ICommandSource
{
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent("Click",
        RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(MenuItem));

    public MenuItem? ParentItem { get; internal set; }

    /// <summary>
    /// Call this method if you have changed menu items set
    /// after menu popup has been shown.
    /// </summary>
    public void ReinitializePopup()
    {
        if (null != _popup)
            _popup.DisconnectMenuItems();
    }

    public event RoutedEventHandler Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    private bool _expanded;

    internal bool Expanded
    {
        get { return _expanded; }
        private set
        {
            if (_expanded != value)
            {
                _expanded = value;
                Invalidate();
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
                Focusable = !_disabled;
                Invalidate();
            }
        }
    }

    public KeyGesture? Gesture { get; set; }

    public bool PopupShadow { get; set; } = true;

    public MenuItem()
    {
        Focusable = true;

        AddHandler(MouseDownEvent, new MouseEventHandler(OnMouseDown));
        AddHandler(MouseMoveEvent, new MouseEventHandler(OnMouseMove));
        AddHandler(MouseUpEvent, new MouseEventHandler(OnMouseUp));
        AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));

        // Stretch by default
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _items.ListChanged += (_, args) =>
        {
            switch (args.Type)
            {
                case ListChangedEventType.ItemsInserted:
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        MenuItemBase itemBase = _items[args.Index + i];
                        if (itemBase is MenuItem item)
                        {
                            item.ParentItem = this;
                        }
                    }

                    break;
                }
                case ListChangedEventType.ItemsRemoved:
                {
                    var removedItems = args.RemovedItems;
                    if (removedItems == null) return;
                    
                    foreach (object? removedItem in removedItems)
                    {
                        if (removedItem is MenuItem item)
                            item.ParentItem = null;
                    }

                    break;
                }
                case ListChangedEventType.ItemReplaced:
                {
                    var removedItems = args.RemovedItems;
                    if (removedItems == null || removedItems.Count < 1) return;
                    
                    object? removedItem = removedItems[0];
                    if (removedItem is MenuItem item)
                        item.ParentItem = null;

                    MenuItemBase itemBase = _items[args.Index];
                    if (itemBase is MenuItem menuItem)
                    {
                        menuItem.ParentItem = this;
                    }

                    break;
                }
            }
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.wVirtualKeyCode == VirtualKeys.Return)
        {
            if (Type == MenuItemType.RootSubmenu || Type == MenuItemType.Submenu)
                OpenMenu();
            else if (Type == MenuItemType.Item)
            {
                RaiseClick();
            }

            args.Handled = true;
        }
    }

    private void OnMouseUp(object sender, MouseEventArgs args)
    {
        if (Type == MenuItemType.Item)
        {
            RaiseClick();
            args.Handled = true;
        }
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
        // Mouse move opens the submenus only in root level
        if (!_disabled && args.LeftButton == MouseButtonState.Pressed /*&& Parent.Parent is Menu*/)
        {
            OpenMenu();
        }

        args.Handled = true;
    }

    private void OnMouseDown(object sender, MouseEventArgs args)
    {
        if (!_disabled)
            OpenMenu();
        args.Handled = true;
    }

    private Popup? _popup;

    private void OpenMenu()
    {
        if (Expanded) return;

        if (Type == MenuItemType.Submenu || Type == MenuItemType.RootSubmenu)
        {
            if (null == _popup)
            {
                _popup = new Popup(Items, PopupShadow, ActualWidth);
                foreach (MenuItemBase itemBase in Items)
                {
                    if (itemBase is MenuItem)
                        ((MenuItem)itemBase).ParentItem = this;
                }

                _popup.AddHandler(Window.ClosedEvent, new EventHandler(OnPopupClosed));
            }

            WindowsHost? windowsHost = VisualTreeHelper.FindClosestParent<WindowsHost>(this);
            Point point = TranslatePoint(this, new Point(0, 0), windowsHost);
            _popup.X = point.X;
            _popup.Y = point.Y;
            windowsHost?.ShowModal(_popup, true);
            Expanded = true;
        }
    }

    private void OnPopupClosed(object? sender, EventArgs eventArgs)
    {
        Assert(Expanded);
        Expanded = false;
    }

    public string? Title { get; set; }

    private string? _titleRight;

    public string? TitleRight
    {
        get
        {
            if (_titleRight == null && Type == MenuItemType.Submenu)
                return new string(UnicodeTable.ArrowRight, 1);
            return _titleRight;
        }
        set => _titleRight = value;
    }

    public string? Description { get; set; }

    public MenuItemType Type { get; set; }

    private readonly ObservableList<MenuItemBase> _items = new ObservableList<MenuItemBase>(new List<MenuItemBase>());

    public IList<MenuItemBase> Items => _items;

    protected override Size MeasureOverride(Size availableSize)
    {
        int length = 2;
        if (!string.IsNullOrEmpty(Title)) length += GetTitleLength(Title);
        if (!string.IsNullOrEmpty(TitleRight)) length += TitleRight.Length;
        if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(TitleRight))
            length++;
        return new Size(length, 1);
    }

    /// <summary>
    /// Counts length of string to be rendered with underscore prefixes on.
    /// </summary>
    private static int GetTitleLength(string? title)
    {
        if (string.IsNullOrEmpty(title))
            return 0;

        bool underscore = false;
        int len = 0;
        foreach (char c in title)
        {
            if (underscore)
            {
                len++;
                underscore = false;
            }
            else
            {
                if (c == '_')
                {
                    underscore = true;
                }
                else
                {
                    len++;
                }
            }
        }

        return len;
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr captionAttrs;
        Attr specialAttrs;
        if (HasFocus || Expanded)
        {
            captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);
            specialAttrs = Colors.Blend(Color.DarkRed, Color.DarkGreen);
        }
        else
        {
            captionAttrs = Colors.Blend(Color.Black, Color.Gray);
            specialAttrs = Colors.Blend(Color.DarkRed, Color.Gray);
        }

        if (_disabled)
            captionAttrs = Colors.Blend(Color.DarkGray, Color.Gray);

        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', captionAttrs);
        if (null != Title)
        {
            RenderString(Title, buffer, 1, 0, ActualWidth, captionAttrs,
                Disabled ? captionAttrs : specialAttrs);
        }

        if (null != TitleRight)
            RenderString(TitleRight, buffer, ActualWidth - TitleRight.Length - 1, 0,
                TitleRight.Length, captionAttrs);
    }

    /// <summary>
    /// Renders string using attr, but if character is prefixed with underscore,
    /// symbol will use specialAttrs instead. To render underscore pass two underscores.
    /// Example: "_File" renders File when 'F' is rendered using specialAttrs.
    /// </summary>
    private static int RenderString(string? s, RenderingBuffer buffer,
        int x, int y, int maxWidth, Attr attr,
        Attr specialAttr)
    {
        if (string.IsNullOrEmpty(s))
            return 0;

        bool underscore = false;
        int j = 0;
        for (int i = 0; i < s.Length && j < maxWidth; i++)
        {
            char c;
            if (underscore)
            {
                c = s[i];
            }
            else
            {
                if (s[i] == '_')
                {
                    underscore = true;
                    continue;
                }

                c = s[i];
            }

            Attr a;
            if (j + 2 >= maxWidth && j >= 2 && s.Length > maxWidth)
            {
                c = '.';
                a = attr;
            }
            else
            {
                a = underscore ? specialAttr : attr;
            }

            buffer.SetPixel(x + j, y, c, a);

            j++;
            underscore = false;
        }

        return j;
    }

    internal class Popup : Window
    {
        private readonly bool shadow;
        private readonly int parentItemWidth; // Размер непрозрачной для нажатий мыши области в 1ой строке окна
        private readonly Panel panel;

        public static readonly RoutedEvent ControlKeyPressedEvent = EventManager.RegisterRoutedEvent(
            "ControlKeyPressed",
            RoutingStrategy.Bubble, typeof(KeyEventHandler), typeof(Popup));

        /// <summary>
        /// Call this method to remove all menu items that are used as child items.
        /// It is necessary before reuse MenuItems in another Popup instance.
        /// </summary>
        public void DisconnectMenuItems()
        {
            panel.Children.Clear();
        }

        /// <summary>
        /// Первая строчка всплывающего окна - особенная. Она прозрачна с точки зрения
        /// рендеринга полностью. Однако Opacity для событий мыши в ней разные.
        /// Первые width пикселей в ней - непрозрачные для событий мыши, но при клике на них
        /// окно закрывается вызовом Close(). Остальные ActualWidth - width пикселей - прозрачные
        /// для событий мыши, и нажатие мыши в этой области приводит к тому, что окно
        /// WindowsHost закрывает окно как окно с OutsideClickClosesWindow = True.
        /// </summary>
        public Popup(IEnumerable<MenuItemBase> menuItems, bool shadow, int parentItemWidth)
        {
            this.parentItemWidth = parentItemWidth;
            this.shadow = shadow;
            panel = new Panel();
            panel.Orientation = Orientation.Vertical;
            foreach (MenuItemBase item in menuItems)
            {
                panel.Children.Add(item);
            }

            Content = panel;

            // If click on the transparent header, close the popup
            AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler((_, args) =>
            {
                if (Content != null && !Content.RenderSlotRect.Contains(args.GetPosition(this)))
                {
                    Close();
                    if (new Rect(new Size(parentItemWidth, 1)).Contains(args.GetPosition(this)))
                    {
                        args.Handled = true;
                    }
                }
            }));

            EventManager.AddHandler(panel, PreviewMouseMoveEvent, new MouseEventHandler(OnPanelMouseMove));
        }

        protected override void OnPreviewKeyDown(object sender, KeyEventArgs args)
        {
            switch (args.wVirtualKeyCode)
            {
                case VirtualKeys.Right:
                {
                    KeyEventArgs newArgs = new KeyEventArgs(this, ControlKeyPressedEvent);
                    newArgs.wVirtualKeyCode = args.wVirtualKeyCode;
                    RaiseEvent(ControlKeyPressedEvent, newArgs);
                    args.Handled = true;
                    break;
                }
                case VirtualKeys.Left:
                {
                    KeyEventArgs newArgs = new KeyEventArgs(this, ControlKeyPressedEvent);
                    newArgs.wVirtualKeyCode = args.wVirtualKeyCode;
                    RaiseEvent(ControlKeyPressedEvent, newArgs);
                    args.Handled = true;
                    break;
                }
                case VirtualKeys.Down:
                    ConsoleApplication.Instance.FocusManager.MoveFocusNext();
                    args.Handled = true;
                    break;
                case VirtualKeys.Up:
                    ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
                    args.Handled = true;
                    break;
                case VirtualKeys.Escape:
                    Close();
                    args.Handled = true;
                    break;
            }
        }

        private void OnPanelMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                PassFocusToChildUnderPoint(e);
            }
        }

        protected override void Initialize()
        {
            AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown), true);
        }

        public override void Render(RenderingBuffer buffer)
        {
            Attr borderAttrs = Colors.Blend(Color.Black, Color.Gray);

            // Background
            buffer.FillRectangle(0, 1, ActualWidth, ActualHeight - 1, ' ', borderAttrs);

            // Первые width пикселей первой строки - прозрачные, но события мыши не пропускают
            // По нажатию на них мы закрываем всплывающее окно вручную
            buffer.SetOpacityRect(0, 0, Math.Min(ActualWidth, parentItemWidth), 1, 2);
            // Оставшиеся пиксели первой строки - пропускают события мыши
            // И WindowsHost закроет всплывающее окно автоматически при нажатии или
            // перемещении нажатого курсора над этим местом
            if (ActualWidth > parentItemWidth)
                buffer.SetOpacityRect(parentItemWidth, 0, ActualWidth - parentItemWidth, 1, 6);

            if (shadow)
            {
                buffer.SetOpacity(0, ActualHeight - 1, 2 + 4);
                buffer.SetOpacity(ActualWidth - 1, 1, 2 + 4);
                buffer.SetOpacityRect(ActualWidth - 1, 2, 1, ActualHeight - 2, 1 + 4);
                buffer.FillRectangle(ActualWidth - 1, 2, 1, ActualHeight - 2, UnicodeTable.FullBlock, borderAttrs);
                buffer.SetOpacityRect(1, ActualHeight - 1, ActualWidth - 1, 1, 3 + 4);
                buffer.FillRectangle(1, ActualHeight - 1, ActualWidth - 1, 1, UnicodeTable.UpperHalfBlock,
                    Attr.NO_ATTRIBUTES);
            }

            RenderBorders(buffer, new Point(1, 1),
                shadow
                    ? new Point(ActualWidth - 3, ActualHeight - 2)
                    : new Point(ActualWidth - 2, ActualHeight - 1),
                true, borderAttrs);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (Content == null) return new Size(0, 0);
            if (shadow)
            {
                // 1 строку и 1 столбец оставляем для прозрачного пространства, остальное занимает Content
                Content.Measure(new Size(availableSize.Width - 3, availableSize.Height - 4));
                // +2 for left empty space and right
                return new Size(Content.DesiredSize.Width + 3 + 2, Content.DesiredSize.Height + 4);
            }
            else
            {
                // 1 строку и 1 столбец оставляем для прозрачного пространства, остальное занимает Content
                Content.Measure(new Size(availableSize.Width - 2, availableSize.Height - 3));
                // +2 for left empty space and right
                return new Size(Content.DesiredSize.Width + 2 + 2, Content.DesiredSize.Height + 3);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Content != null)
            {
                if (shadow)
                {
                    // 1 pixel from all borders - for popup padding
                    // 1 pixel from top - for transparent region
                    // Additional pixel from right and bottom - for shadow
                    Content.Arrange(new Rect(new Point(2, 2),
                        new Size(finalSize.Width - 5, finalSize.Height - 4)));
                }
                else
                {
                    // 1 pixel from all borders - for popup padding
                    // 1 pixel from top - for transparent region
                    Content.Arrange(new Rect(new Point(2, 2),
                        new Size(finalSize.Width - 4, finalSize.Height - 3)));
                }
            }

            return finalSize;
        }
    }

    internal void Close()
    {
        Assert(Expanded);
        _popup?.Close();
    }

    internal void Expand()
    {
        OpenMenu();
    }

    private ICommand? command;

    public ICommand? Command
    {
        get => command;
        set
        {
            if (command != value)
            {
                if (command != null)
                {
                    command.CanExecuteChanged -= OnCommandCanExecuteChanged;
                }

                command = value;
                if (command != null) 
                    command.CanExecuteChanged += OnCommandCanExecuteChanged;

                RefreshCanExecute();
            }
        }
    }

    private void OnCommandCanExecuteChanged(object? sender, EventArgs args)
    {
        RefreshCanExecute();
    }

    private void RefreshCanExecute()
    {
        if (command == null)
        {
            Disabled = false;
            return;
        }

        Disabled = !command.CanExecute(CommandParameter);
    }

    public object? CommandParameter { get; set; }

    internal void RaiseClick()
    {
        RaiseEvent(ClickEvent, new RoutedEventArgs(this, ClickEvent));
        if (command != null && command.CanExecute(CommandParameter))
        {
            command.Execute(CommandParameter);
        }
    }
}

/// <summary>
/// Cannot be added in root menu.
/// </summary>
public class Separator : MenuItemBase
{
    public Separator()
    {
        Focusable = false;

        // Stretch by default
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(1, 1);
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr captionAttrs;
        if (HasFocus)
            captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);
        else
            captionAttrs = Colors.Blend(Color.Black, Color.Gray);

        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, UnicodeTable.SingleFrameHorizontal, captionAttrs);
    }
}

public class Menu : Control
{
    private readonly ObservableList<MenuItemBase> items = new ObservableList<MenuItemBase>(
        new List<MenuItemBase>());

    public IList<MenuItemBase> Items => items;

    private void GetGestures(MenuItem item, Dictionary<KeyGesture, MenuItem> map)
    {
        if (item.Gesture != null)
            map.Add(item.Gesture, item);
        if (item.Type == MenuItemType.RootSubmenu ||
            item.Type == MenuItemType.Submenu)
        {
            foreach (MenuItemBase itemBase in item.Items)
            {
                if (itemBase is MenuItem)
                {
                    GetGestures((MenuItem)itemBase, map);
                }
            }
        }
    }

    public void RefreshKeyGestures()
    {
        _gestures = null;
    }

    private Dictionary<KeyGesture, MenuItem>? _gestures;

    private Dictionary<KeyGesture, MenuItem> GetGesturesMap()
    {
        if (_gestures == null)
        {
            _gestures = new Dictionary<KeyGesture, MenuItem>();
            foreach (MenuItemBase itemBase in Items)
            {
                if (itemBase is MenuItem)
                {
                    GetGestures((MenuItem)itemBase, _gestures);
                }
            }
        }

        return _gestures;
    }

    public bool TryMatchGesture(KeyEventArgs args)
    {
        Dictionary<KeyGesture, MenuItem> map = GetGesturesMap();
        KeyGesture? match = map.Keys.FirstOrDefault(gesture => gesture.Matches(args));
        if (match == null) return false;

        CloseAllSubmenus();

        // Activate matches menu item
        MenuItem menuItem = map[match];
        List<MenuItem> path = new List<MenuItem>();
        MenuItem? currentItem = menuItem;
        while (currentItem != null)
        {
            path.Add(currentItem);
            currentItem = currentItem.ParentItem;
        }

        path.Reverse();

        // Open all menu items in path successively
        int i = 0;
        Action? action = null;
        action = () =>
        {
            if (i < path.Count)
            {
                MenuItem item = path[i];
                if (item.Type == MenuItemType.Item)
                {
                    item.RaiseClick();
                    return;
                }

                // Activate focus on item
                if (item.ParentItem == null)
                {
                    ConsoleApplication.Instance.FocusManager.SetFocus(this, item);
                }
                else
                {
                    var parent = item.Parent;
                    if (parent != null)
                    {
                        parent = item.Parent;
                        if (parent != null)
                            // Set focus to PopupWindow -> item
                            ConsoleApplication.Instance.FocusManager.SetFocus(
                                parent, item);
                    }
                }

                item.Invalidate();
                EventHandler? handler = null;

                // Wait for layout to be revalidated and expand it
                handler = (_, _) =>
                {
                    item.Expand();
                    item.LayoutRevalidated -= handler;
                    i++;
                    if (i < path.Count)
                    {
                        if (action != null)
                            action();
                    }
                };
                item.LayoutRevalidated += handler;
            }
        };
        action();

        return true;
    }

    /// <summary>
    /// Forces all open submenus to be closed.
    /// </summary>
    public void CloseAllSubmenus()
    {
        List<MenuItem> expandedSubmenus = new List<MenuItem>();
        MenuItem? currentItem =
            (MenuItem?)Items.SingleOrDefault(item => item is MenuItem && ((MenuItem)item).Expanded);
        while (null != currentItem)
        {
            expandedSubmenus.Add(currentItem);
            currentItem =
                (MenuItem?)currentItem.Items.SingleOrDefault(item => item is MenuItem && ((MenuItem)item).Expanded);
        }

        expandedSubmenus.Reverse();
        foreach (MenuItem expandedSubmenu in expandedSubmenus)
        {
            expandedSubmenu.Close();
        }
    }

    public Menu()
    {
        Panel stackPanel = new Panel();
        stackPanel.Orientation = Orientation.Horizontal;
        AddChild(stackPanel);

        // Subscribe to Items change and add to Children them
        items.ListChanged += (_, args) =>
        {
            switch (args.Type)
            {
                case ListChangedEventType.ItemsInserted:
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        MenuItemBase item = items[args.Index + i];
                        if (item is Separator)
                            throw new InvalidOperationException("Separator cannot be added to root menu.");
                        if (((MenuItem)item).Type == MenuItemType.Submenu)
                            ((MenuItem)item).Type = MenuItemType.RootSubmenu;
                        stackPanel.Children.Insert(args.Index + i, item);
                    }

                    break;
                }
                case ListChangedEventType.ItemsRemoved:
                    for (int i = 0; i < args.Count; i++)
                        stackPanel.Children.RemoveAt(args.Index);
                    break;
                case ListChangedEventType.ItemReplaced:
                {
                    MenuItemBase item = items[args.Index];
                    if (item is Separator)
                        throw new InvalidOperationException("Separator cannot be added to root menu.");
                    if (((MenuItem)item).Type == MenuItemType.Submenu)
                        ((MenuItem)item).Type = MenuItemType.RootSubmenu;
                    stackPanel.Children[args.Index] = item;
                    break;
                }
            }
        };
        IsFocusScope = true;

        AddHandler(KeyDownEvent, new KeyEventHandler(OnKeyDown));
        AddHandler(PreviewMouseMoveEvent, new MouseEventHandler(OnPreviewMouseMove));
        AddHandler(PreviewMouseDownEvent, new MouseEventHandler(OnPreviewMouseDown));
    }

    protected override void OnParentChanged()
    {
        if (Parent != null)
        {
            Assert(Parent is WindowsHost);

            // Вешаем на WindowsHost обработчик события MenuItem.ClickEvent,
            // чтобы ловить момент выбора пункта меню в одном из модальных всплывающих окошек
            // Дело в том, что эти окошки не являются дочерними элементами контрола Menu,
            // а напрямую являются дочерними элементами WindowsHost (т.к. именно он создаёт
            // окна). И событие выбора пункта меню из всплывающего окошка может быть поймано 
            // в WindowsHost, но не в Menu. А нам нужно повесить обработчик, который закроет
            // все показанные попапы.
            EventManager.AddHandler(Parent, MenuItem.ClickEvent,
                new RoutedEventHandler((_, _) => CloseAllSubmenus()), true);

            EventManager.AddHandler(Parent, MenuItem.Popup.ControlKeyPressedEvent,
                new KeyEventHandler((_, args) =>
                {
                    CloseAllSubmenus();
                    //
                    ConsoleApplication.Instance.FocusManager.SetFocusScope(this);
                    if (args.wVirtualKeyCode == VirtualKeys.Right)
                        ConsoleApplication.Instance.FocusManager.MoveFocusNext();
                    else if (args.wVirtualKeyCode == VirtualKeys.Left)
                        ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
                    MenuItem? focusedItem =
                        (MenuItem?)Items.SingleOrDefault(item => item is MenuItem && item.HasFocus);
                    focusedItem?.Expand();
                }));
        }
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs args)
    {
        if (args.LeftButton == MouseButtonState.Pressed)
        {
            OnPreviewMouseDown(sender, args);
        }
    }

    private void OnPreviewMouseDown(object sender, MouseEventArgs e)
    {
        PassFocusToChildUnderPoint(e);
    }

    private void OnKeyDown(object sender, KeyEventArgs args)
    {
        if (args.wVirtualKeyCode == VirtualKeys.Right)
        {
            ConsoleApplication.Instance.FocusManager.MoveFocusNext();
            args.Handled = true;
        }

        if (args.wVirtualKeyCode == VirtualKeys.Left)
        {
            ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
            args.Handled = true;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Children[0].Measure(availableSize);
        return Children[0].DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        Children[0].Arrange(new Rect(new Point(0, 0), finalSize));
        return finalSize;
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.Black, Color.Gray);
        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
    }
}