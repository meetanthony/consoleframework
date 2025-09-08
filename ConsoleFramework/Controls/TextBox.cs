using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// todo : добавить обработку выделения текста
/// </summary>
public class TextBox : Control
{
    public TextBox()
    {
        KeyDown += TextBox_KeyDown;
        MouseDown += OnMouseDown;
        CursorVisible = true;
        CursorPosition = new Point(1, 0);
        Focusable = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        Point point = args.GetPosition(this);
        if (point.X > 0 && point.X - 1 < GetSize())
        {
            int x = point.X - 1;
            if (!String.IsNullOrEmpty(_text))
            {
                if (x <= _text.Length)
                    _cursorPosition = x;
                else
                {
                    _cursorPosition = _text.Length;
                }

                CursorPosition = new Point(_cursorPosition + 1, 0);
            }

            args.Handled = true;
        }
    }

    private void TextBox_KeyDown(object sender, KeyEventArgs args)
    {
        ConsoleKeyInfo keyInfo = new ConsoleKeyInfo(args.UnicodeChar,
            (ConsoleKey)args.wVirtualKeyCode,
            (args.dwControlKeyState & ControlKeyState.SHIFT_PRESSED) == ControlKeyState.SHIFT_PRESSED,
            (args.dwControlKeyState & ControlKeyState.LEFT_ALT_PRESSED) == ControlKeyState.LEFT_ALT_PRESSED
            || (args.dwControlKeyState & ControlKeyState.RIGHT_ALT_PRESSED) == ControlKeyState.RIGHT_ALT_PRESSED,
            (args.dwControlKeyState & ControlKeyState.LEFT_CTRL_PRESSED) == ControlKeyState.LEFT_CTRL_PRESSED
            || (args.dwControlKeyState & ControlKeyState.RIGHT_CTRL_PRESSED) == ControlKeyState.RIGHT_CTRL_PRESSED
        );
        if (!char.IsControl(keyInfo.KeyChar))
        {
            // insert keychar into a text string according to cursorPosition and offset
            if (_text != null)
            {
                string leftPart = _text.Substring(0, _cursorPosition + _displayOffset);
                string rightPart = _text.Substring(_cursorPosition + _displayOffset);
                Text = leftPart + keyInfo.KeyChar + rightPart;
            }
            else
            {
                Text = keyInfo.KeyChar.ToString();
            }

            if (_cursorPosition + 1 < ActualWidth - 2)
            {
                _cursorPosition++;
                CursorPosition = new Point(_cursorPosition + 1, 0);
            }
            else
            {
                _displayOffset++;
            }
        }
        else
        {
            if (keyInfo.Key == ConsoleKey.Delete)
            {
                if (!String.IsNullOrEmpty(_text) && _displayOffset + _cursorPosition < _text.Length)
                {
                    string leftPart = _text.Substring(0, _cursorPosition + _displayOffset);
                    string rightPart = _text.Substring(_cursorPosition + _displayOffset + 1);
                    Text = leftPart + rightPart;
                    //
                }
                else
                {
                    Console.Beep();
                }
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (!String.IsNullOrEmpty(_text) && (_displayOffset != 0 || _cursorPosition != 0))
                {
                    string leftPart = _text.Substring(0, _cursorPosition + _displayOffset - 1);
                    string rightPart = _text.Substring(_cursorPosition + _displayOffset);
                    Text = leftPart + rightPart;
                    if (_displayOffset > 0)
                        _displayOffset--;
                    else
                    {
                        if (_cursorPosition > 0)
                        {
                            _cursorPosition--;
                            CursorPosition = new Point(_cursorPosition + 1, 0);
                        }
                    }
                }
                else
                {
                    Console.Beep();
                }
            }

            if (keyInfo.Key == ConsoleKey.LeftArrow)
            {
                if ((keyInfo.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    // todo :
                }

                if (!String.IsNullOrEmpty(_text) && (_displayOffset != 0 || _cursorPosition != 0))
                {
                    if (_cursorPosition > 0)
                    {
                        _cursorPosition--;
                        CursorPosition = new Point(_cursorPosition + 1, 0);
                    }
                    else
                    {
                        if (_displayOffset > 0)
                        {
                            _displayOffset--;
                            Invalidate();
                        }
                    }
                }
                else
                {
                    Console.Beep();
                }
            }

            if (keyInfo.Key == ConsoleKey.RightArrow)
            {
                if (!String.IsNullOrEmpty(_text) && _displayOffset + _cursorPosition < _text.Length)
                {
                    if (_cursorPosition + 1 < ActualWidth - 2)
                    {
                        _cursorPosition++;
                        CursorPosition = new Point(_cursorPosition + 1, 0);
                    }
                    else
                    {
                        if (_displayOffset + _cursorPosition < _text.Length)
                        {
                            _displayOffset++;
                            Invalidate();
                        }
                    }
                }
                else
                {
                    Console.Beep();
                }
            }

            if (keyInfo.Key == ConsoleKey.Home)
            {
                if (_displayOffset != 0 || _cursorPosition != 0)
                {
                    _displayOffset = 0;
                    _cursorPosition = 0;
                    CursorPosition = new Point(_cursorPosition + 1, 0);
                    Invalidate();
                }
                else
                {
                    Console.Beep();
                }
            }

            if (keyInfo.Key == ConsoleKey.End)
            {
                if (!String.IsNullOrEmpty(_text) && _cursorPosition + _displayOffset < ActualWidth - 2)
                {
                    _displayOffset = _text.Length >= ActualWidth - 2 ? _text.Length - (ActualWidth - 2) + 1 : 0;
                    _cursorPosition = _text.Length >= ActualWidth - 2 ? ActualWidth - 2 - 1 : _text.Length;
                    CursorPosition = new Point(_cursorPosition + 1, 0);
                    Invalidate();
                }
                else
                {
                    Console.Beep();
                }
            }
        }
        //Debugger.Log(0, "", String.Format("cursorPos : {0} offset {1}\n", cursorPosition, displayOffset));
    }

    private string? _text;

    public string? Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                Invalidate();
                RaisePropertyChanged("Text");
            }
        }
    }

    public int MaxLength { get; set; }

    public int? Size { get; set; }

    public char? PasswordChar { get; set; }

    private int GetSize()
    {
        if (Size.HasValue) return Size.Value;
        return _text != null ? _text.Length + 1 : 1;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size desired = new Size(GetSize() + 2, 1);
        return new Size(
            Math.Min(desired.Width, availableSize.Width),
            Math.Min(desired.Height, availableSize.Height)
        );
    }

    // this fields describe the whole state of textbox
    private int _displayOffset;

    private int _cursorPosition;

    // -1 if no selection started
    private int _startSelection;

    public override void Render(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.White, Color.DarkBlue);
        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
        if (null != _text)
        {
            for (int i = _displayOffset; i < _text.Length; i++)
            {
                if (i - _displayOffset < ActualWidth - 2 && i - _displayOffset >= 0)
                {
                    buffer.SetPixel(1 + i - _displayOffset, 0, PasswordChar.HasValue ? PasswordChar.Value : _text[i]);
                }
            }
        }

        Attr arrowsAttr = Colors.Blend(Color.Green, Color.DarkBlue);
        if (_displayOffset > 0)
            buffer.SetPixel(0, 0, '<', arrowsAttr);
        if (!String.IsNullOrEmpty(_text) && ActualWidth - 2 + _displayOffset < _text.Length)
            buffer.SetPixel(ActualWidth - 1, 0, '>', arrowsAttr);
    }
}