using System;
using System.Collections.Generic;
using System.Linq;
using ConsoleFramework.Binding.Observables;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.Controls;

[ContentProperty("Items")]
public class ContextMenu
{
    private readonly ObservableList<MenuItemBase> _items = new(new List<MenuItemBase>());

    public IList<MenuItemBase> Items => _items;

    private MenuItem.Popup? _popup;
    private bool _expanded;

    private bool _popupShadow = true;

    public bool PopupShadow
    {
        get => _popupShadow;
        set => _popupShadow = value;
    }

    /// <summary>
    /// Forces all open submenus to be closed.
    /// </summary>
    public void CloseAllSubmenus()
    {
        List<MenuItem> expandedSubmenus = new List<MenuItem>();
        MenuItem? currentItem =
            (MenuItem?)Items.SingleOrDefault(item => item is MenuItem && ((MenuItem)item).expanded);
        while (null != currentItem)
        {
            expandedSubmenus.Add(currentItem);
            currentItem =
                (MenuItem?)currentItem.Items.SingleOrDefault(item => item is MenuItem && ((MenuItem)item).expanded);
        }

        expandedSubmenus.Reverse();
        foreach (MenuItem expandedSubmenu in expandedSubmenus)
        {
            expandedSubmenu.Close();
        }
    }

    private WindowsHost? _windowsHost;
    private RoutedEventHandler? _windowsHostClick;
    private KeyEventHandler? _windowsHostControlKeyPressed;

    public void OpenMenu(WindowsHost windowsHost, Point point)
    {
        if (_expanded) return;

        // Вешаем на WindowsHost обработчик события MenuItem.ClickEvent,
        // чтобы ловить момент выбора пункта меню в одном из модальных всплывающих окошек
        // Дело в том, что эти окошки не являются дочерними элементами контрола Menu,
        // а напрямую являются дочерними элементами WindowsHost (т.к. именно он создаёт
        // окна). И событие выбора пункта меню из всплывающего окошка может быть поймано 
        // в WindowsHost, но не в Menu. А нам нужно повесить обработчик, который закроет
        // все показанные попапы.
        EventManager.AddHandler(windowsHost, MenuItem.ClickEvent,
            _windowsHostClick = (sender, args) =>
            {
                CloseAllSubmenus();
                _popup?.Close();
            }, true);

        EventManager.AddHandler(windowsHost, MenuItem.Popup.ControlKeyPressedEvent,
            _windowsHostControlKeyPressed = (sender, args) =>
            {
                CloseAllSubmenus();
                //
                //ConsoleApplication.Instance.FocusManager.SetFocusScope(this);
                if (args.wVirtualKeyCode == VirtualKeys.Right)
                    ConsoleApplication.Instance.FocusManager.MoveFocusNext();
                else if (args.wVirtualKeyCode == VirtualKeys.Left)
                    ConsoleApplication.Instance.FocusManager.MoveFocusPrev();
                MenuItem? focusedItem = (MenuItem?)Items.SingleOrDefault(item => item is MenuItem && item.HasFocus);
                focusedItem?.Expand();
            });

        if (null == _popup)
        {
            _popup = new MenuItem.Popup(Items, _popupShadow, 0);
            _popup.AddHandler(Window.ClosedEvent, new EventHandler(OnPopupClosed));
        }

        _popup.X = point.X;
        _popup.Y = point.Y;
        windowsHost.ShowModal(_popup, true);
        _expanded = true;
        _windowsHost = windowsHost;
    }

    private void OnPopupClosed(object? sender, EventArgs eventArgs)
    {
        if (!_expanded) throw new InvalidOperationException("This shouldn't happen");
        _expanded = false;
        if (_windowsHost != null)
        {
            if (_windowsHostClick != null)
                EventManager.RemoveHandler(_windowsHost, MenuItem.ClickEvent, _windowsHostClick);
            if (_windowsHostControlKeyPressed != null)
                EventManager.RemoveHandler(_windowsHost, MenuItem.Popup.ControlKeyPressedEvent,
                    _windowsHostControlKeyPressed);
        }
    }
}