using System;
using ConsoleFramework.Native;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.Events;

[TypeConverter(typeof(KeyGestureConverter))]
public class KeyGesture
{
    private readonly string _displayString;
    private readonly VirtualKeys _key;
    private static readonly ITypeConverter KeyGestureConverter = new KeyGestureConverter();
    private readonly ModifierKeys _modifiers;

    public KeyGesture(VirtualKeys key) : this(key, ModifierKeys.None)
    {
    }

    public KeyGesture(VirtualKeys key, ModifierKeys modifiers) : this(key, modifiers, string.Empty)
    {
    }

    public KeyGesture(VirtualKeys key, ModifierKeys modifiers, string displayString)
    {
        if (displayString == null) throw new ArgumentNullException(nameof(displayString));
        if (!IsValid(key, modifiers))
            throw new InvalidOperationException("KeyGesture is invalid");
        _modifiers = modifiers;
        _key = key;
        _displayString = displayString;
    }

    public string GetDisplayString()
    {
        if (!string.IsNullOrEmpty(_displayString))
        {
            return _displayString;
        }

        return (string)KeyGestureConverter.ConvertTo(this, typeof(string));
    }

    // todo : check incompatible combinations
    internal static bool IsValid(VirtualKeys key, ModifierKeys modifiers)
    {
        return true;
    }

    public bool Matches(KeyEventArgs args)
    {
        VirtualKeys wVirtualKeyCode = args.wVirtualKeyCode;
        if (Key != wVirtualKeyCode) return false;
        ControlKeyState controlKeyState = args.dwControlKeyState;
        ModifierKeys modifierKeys = Modifiers;

        // Проверяем все возможные модификаторы по очереди

        if ((modifierKeys & ModifierKeys.Alt) != 0)
        {
            if ((controlKeyState & (ControlKeyState.LEFT_ALT_PRESSED
                                    | ControlKeyState.RIGHT_ALT_PRESSED)) == 0)
            {
                // Должен быть взведён один из флагов, показывающих нажатие Alt, а его нет
                return false;
            }
        }
        else
        {
            if ((controlKeyState & (ControlKeyState.LEFT_ALT_PRESSED
                                    | ControlKeyState.RIGHT_ALT_PRESSED)) != 0)
            {
                // Не должно быть взведено ни одного флага, показывающего нажатие Alt,
                // а на самом деле - флаг стоит
                return false;
            }
        }

        if ((modifierKeys & ModifierKeys.Control) != 0)
        {
            if ((controlKeyState & (ControlKeyState.LEFT_CTRL_PRESSED
                                    | ControlKeyState.RIGHT_CTRL_PRESSED)) == 0)
            {
                return false;
            }
        }
        else
        {
            if ((controlKeyState & (ControlKeyState.LEFT_CTRL_PRESSED
                                    | ControlKeyState.RIGHT_CTRL_PRESSED)) != 0)
            {
                return false;
            }
        }

        if ((modifierKeys & ModifierKeys.Shift) != 0)
        {
            if ((controlKeyState & ControlKeyState.SHIFT_PRESSED) == 0)
            {
                return false;
            }
        }
        else
        {
            if ((controlKeyState & ControlKeyState.SHIFT_PRESSED) != 0)
            {
                return false;
            }
        }

        return true;
    }

    public string DisplayString => _displayString;

    public VirtualKeys Key => _key;

    public ModifierKeys Modifiers => _modifiers;
}