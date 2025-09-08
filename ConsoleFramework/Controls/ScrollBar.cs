using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

public class ScrollBarValueChanged : RoutedEventArgs
{
    public ScrollBarValueChanged(object source, RoutedEvent routedEvent) : base(source, routedEvent)
    {
    }
}

public delegate void ScrollBarValueChangedEventHandler(object sender, ScrollBarValueChanged args);

/// <summary>
/// Auxiliary control only displaying the scroll bar.
/// </summary>
public class ScrollBar : Control
{
    public static RoutedEvent ScrollBarValueChangedEvent =
        EventManager.RegisterRoutedEvent("ScrollBarValueChanged", RoutingStrategy.Bubble,
            typeof(ScrollBarValueChangedEventHandler), typeof(ScrollBar));

    private Orientation _orientation = Orientation.Horizontal;

    public Orientation Orientation
    {
        get => _orientation;
        set
        {
            if (_orientation != value)
            {
                _orientation = value;
                Invalidate();
            }
        }
    }

    private int _value;

    public int Value
    {
        get => _value;
        set
        {
            if (value != _value)
            {
                _value = Math.Min(_maxValue, value);
                Invalidate();
            }
        }
    }

    private int _maxValue = 100;

    public int MaxValue
    {
        get => _maxValue;
        set
        {
            if (value != _maxValue)
            {
                _maxValue = value;
                _value = Math.Min(_value, _maxValue);
                Invalidate();
            }
        }
    }

    public ScrollBar()
    {
        // Control doesn't work with another modes
        // because it requires you arrange it with actual size, but since
        // MeasureOverride() is not implemented, the Arrange() method will be called
        // with (0, 0) in another modes
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
    }

    public override void Render(RenderingBuffer buffer)
    {
        if (Orientation == Orientation.Horizontal)
        {
            RenderHorizontal(buffer);
        }
        else
        {
            RenderVertical(buffer);
        }
    }

    private void RenderHorizontal(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.DarkCyan, Color.DarkBlue);
        if (ActualWidth >= 1)
        {
            buffer.FillRectangle(0, 0, 1, ActualHeight, UnicodeTable.ArrowLeft, attr); // ◄
        }

        if (ActualWidth >= 2)
        {
            buffer.FillRectangle(ActualWidth - 1, 0, 1, ActualHeight, UnicodeTable.ArrowRight, attr); // ►
        }

        if (ActualWidth >= 3)
        {
            buffer.FillRectangle(1, 0, ActualWidth - 2, ActualHeight, UnicodeTable.MediumShade, attr); // ▒
            buffer.FillRectangle(GetCurrentPage() + 1, 0, 1, ActualHeight, UnicodeTable.BlackSquare, attr); // ■
        }
    }

    private void RenderVertical(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.DarkCyan, Color.DarkBlue);
        if (ActualHeight >= 1)
        {
            buffer.FillRectangle(0, 0, ActualWidth, 1, UnicodeTable.ArrowUp, attr); // ▲
        }

        if (ActualHeight >= 2)
        {
            buffer.FillRectangle(0, ActualHeight - 1, ActualWidth, 1, UnicodeTable.ArrowDown, attr); // ▼
        }

        if (ActualHeight >= 3)
        {
            buffer.FillRectangle(0, 1, ActualWidth, ActualHeight - 2, UnicodeTable.MediumShade, attr); // ▒
            buffer.FillRectangle(0, GetCurrentPage() + 1, ActualWidth, 1, UnicodeTable.BlackSquare, attr); // ■
        }
    }

    private int GetPagesCount()
    {
        if (Orientation == Orientation.Horizontal)
        {
            return Math.Max(1, ActualWidth - 2);
        }

        return Math.Max(1, ActualHeight - 2);
    }

    /// <summary>
    /// Returns page which scroller points to
    /// </summary>
    private int GetCurrentPage()
    {
        return (int)Math.Truncate(1.0 * Value / MaxValue * (GetPagesCount() - 1));
    }
}