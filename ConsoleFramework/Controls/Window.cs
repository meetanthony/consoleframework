using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// Window is a control that can hold one child only.
/// Usually a child is some panel or grid (or another layout control)
/// Window exists in WindowsHost instance, so Window should be aware about WindowsHost
/// and should be able to interoperate with it.
/// </summary>
public class Window : Control
{
    public static RoutedEvent ActivatedEvent =
        EventManager.RegisterRoutedEvent("Activated", RoutingStrategy.Direct, typeof(EventHandler), typeof(Window));

    public static RoutedEvent DeactivatedEvent = EventManager.RegisterRoutedEvent("Deactivated", RoutingStrategy.Direct,
        typeof(EventHandler), typeof(Window));

    public static RoutedEvent ClosedEvent =
        EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Direct, typeof(EventHandler), typeof(Window));

    public static RoutedEvent ClosingEvent = EventManager.RegisterRoutedEvent("Closing", RoutingStrategy.Direct,
        typeof(CancelEventHandler), typeof(Window));

    //public string ChildToFocus { get; set; }

    public Window()
    {
        IsFocusScope = true;
        AddHandler(PreviewMouseDownEvent, new MouseButtonEventHandler(Window_OnPreviewMouseDown));
        Initialize();
    }

    private void Initialize()
    {
        AddHandler(MouseDownEvent, new MouseButtonEventHandler(Window_OnMouseDown));
        AddHandler(MouseUpEvent, new MouseButtonEventHandler(Window_OnMouseUp));
        AddHandler(MouseMoveEvent, new MouseEventHandler(Window_OnMouseMove));
        AddHandler(PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
        AddHandler(ActivatedEvent, new EventHandler(Window_OnActivated));
        AddHandler(DeactivatedEvent, new EventHandler(Window_OnDeactivated));

        InitEvents();
    }

    protected virtual void InitEvents()
    {
        
    }
    
    /// <summary>
    /// Handles the mouse click: founds the end Focusable element which
    /// is placed under mouse and sets the focus to it.
    /// </summary>
    private void Window_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        PassFocusToChildUnderPoint(e);
    }

    protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs args)
    {
        if (args.wVirtualKeyCode == VirtualKeys.Tab)
        {
            ConsoleApplication.Instance.FocusManager.MoveFocusNext();
            args.Handled = true;
        }
    }

    /// <summary>
    /// Window coords in WindowsHost. In fact it is poor man attached properties.
    /// </summary>
    public int? X { get; set; }

    public int? Y { get; set; }

    public Control? Content
    {
        get => Children.Count != 0 ? Children[0] : null;
        set
        {
            if (Children.Count != 0)
            {
                RemoveChild(Children[0]);
            }

            if (value != null)
                AddChild(value);
        }
    }

    private string _title = String.Empty;

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                _title = value;
                Invalidate();
                RaisePropertyChanged("Title");
            }
        }
    }

    private ColorPair? _activeBorderColors;

    /// <summary>
    /// Special colors for active window. Not set by default.
    /// </summary>
    public ColorPair? ActiveBorderColors
    {
        get => _activeBorderColors;
        set
        {
            if (!Equals(_activeBorderColors, value))
            {
                _activeBorderColors = value;
                Invalidate();
            }
        }
    }

    protected WindowsHost? GetWindowsHost()
    {
        return Parent as WindowsHost;
    }

    public static Size EmptyWindowSize = new Size(12, 3);

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content == null)
        {
            return new Size(
                Math.Min(availableSize.Width, EmptyWindowSize.Width + 4),
                Math.Min(availableSize.Height, EmptyWindowSize.Height + 3)
            );
        }

        // Reserve 2 pixels for frame and 2/1 pixels for shadow
        int width = availableSize.Width != int.MaxValue ? Math.Max(4, availableSize.Width) - 4 : int.MaxValue;
        int height = availableSize.Height != int.MaxValue ? Math.Max(3, availableSize.Height) - 3 : int.MaxValue;
        Content.Measure(new Size(width, height));

        // Avoid int overflow. Additional -1 to avoid returning int.MaxValue from MeasureOverride (by contract)
        var result = new Size(
            Math.Min(int.MaxValue - 4 - 1, Content.DesiredSize.Width) + 4,
            Math.Min(int.MaxValue - 3 - 1, Content.DesiredSize.Height) + 3
        );
        return result;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Content != null)
        {
            Content.Arrange(new Rect(1, 1,
                Math.Max(4, finalSize.Width) - 4,
                Math.Max(3, finalSize.Height) - 3));
        }

        return finalSize;
    }

    protected void RenderBorders(RenderingBuffer buffer, Point a, Point b, bool singleOrDouble, Attr attrs)
    {
        if (singleOrDouble)
        {
            // Corners
            buffer.SetPixel(a.X, a.Y, UnicodeTable.SingleFrameTopLeftCorner, attrs);
            buffer.SetPixel(b.X, b.Y, UnicodeTable.SingleFrameBottomRightCorner, attrs);
            buffer.SetPixel(a.X, b.Y, UnicodeTable.SingleFrameBottomLeftCorner, attrs);
            buffer.SetPixel(b.X, a.Y, UnicodeTable.SingleFrameTopRightCorner, attrs);
            // Horizontal & vertical frames
            buffer.FillRectangle(a.X + 1, a.Y, b.X - a.X - 1, 1, UnicodeTable.SingleFrameHorizontal, attrs);
            buffer.FillRectangle(a.X + 1, b.Y, b.X - a.X - 1, 1, UnicodeTable.SingleFrameHorizontal, attrs);
            buffer.FillRectangle(a.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.SingleFrameVertical, attrs);
            buffer.FillRectangle(b.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.SingleFrameVertical, attrs);
        }
        else
        {
            // Corners
            buffer.SetPixel(a.X, a.Y, UnicodeTable.DoubleFrameTopLeftCorner, attrs);
            buffer.SetPixel(b.X, b.Y, UnicodeTable.DoubleFrameBottomRightCorner, attrs);
            buffer.SetPixel(a.X, b.Y, UnicodeTable.DoubleFrameBottomLeftCorner, attrs);
            buffer.SetPixel(b.X, a.Y, UnicodeTable.DoubleFrameTopRightCorner, attrs);
            // Horizontal & vertical frames
            buffer.FillRectangle(a.X + 1, a.Y, b.X - a.X - 1, 1, UnicodeTable.DoubleFrameHorizontal, attrs);
            buffer.FillRectangle(a.X + 1, b.Y, b.X - a.X - 1, 1, UnicodeTable.DoubleFrameHorizontal, attrs);
            buffer.FillRectangle(a.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.DoubleFrameVertical, attrs);
            buffer.FillRectangle(b.X, a.Y + 1, 1, b.Y - a.Y - 1, UnicodeTable.DoubleFrameVertical, attrs);
        }
    }

    public bool IsActiveWindow()
    {
        return GetWindowsHost()?.TopWindow == this;
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr borderAttrs = _moving ? Colors.Blend(Color.Green, Color.Gray) : Colors.Blend(Color.White, Color.Gray);
        if (ActiveBorderColors != null && IsActiveWindow())
        {
            borderAttrs = Colors.Blend(ActiveBorderColors.ForegroundColor, ActiveBorderColors.BackgroundColor);
        }

        // background
        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', borderAttrs);
        // Borders
        Point bottomRight = new Point(ActualWidth - 3, ActualHeight - 2);
        RenderBorders(buffer, new Point(0, 0), bottomRight, _moving || _resizing, borderAttrs);
        // Additional green right bottom corner if resizing
        if (_resizing)
        {
            buffer.SetPixel(bottomRight.X, bottomRight.Y, UnicodeTable.SingleFrameBottomRightCorner,
                Colors.Blend(Color.Green, Color.Gray));
        }

        // close button
        if (ActualWidth > 4)
        {
            buffer.SetPixel(2, 0, '[');
            buffer.SetPixel(3, 0,
                _showClosingGlyph ? UnicodeTable.WindowClosePressedSymbol : UnicodeTable.WindowCloseSymbol,
                Colors.Blend(Color.Green, Color.Gray));
            buffer.SetPixel(4, 0, ']');
        }

        // shadows
        buffer.SetOpacity(0, ActualHeight - 1, 2 + 4);
        buffer.SetOpacity(1, ActualHeight - 1, 2 + 4);
        buffer.SetOpacity(ActualWidth - 1, 0, 2 + 4);
        buffer.SetOpacity(ActualWidth - 2, 0, 2 + 4);
        buffer.SetOpacityRect(2, ActualHeight - 1, ActualWidth - 2, 1, 1 + 4);
        buffer.SetOpacityRect(ActualWidth - 2, 1, 2, ActualHeight - 1, 1 + 4);
        // title
        if (!string.IsNullOrEmpty(Title))
        {
            int titleStartX = 7;
            bool renderTitle = false;
            string renderTitleString = String.Empty;
            int availablePixelsCount = ActualWidth - titleStartX * 2;
            if (availablePixelsCount > 0)
            {
                renderTitle = true;
                if (Title.Length <= availablePixelsCount)
                {
                    // dont truncate title
                    titleStartX += (availablePixelsCount - Title.Length) / 2;
                    renderTitleString = Title;
                }
                else
                {
                    renderTitleString = Title.Substring(0, availablePixelsCount);
                    if (renderTitleString.Length > 2)
                    {
                        renderTitleString = renderTitleString.Substring(0, renderTitleString.Length - 2) + "..";
                    }
                    else
                    {
                        renderTitle = false;
                    }
                }
            }

            if (renderTitle)
            {
                // assert !string.IsNullOrEmpty(renderingTitleString);
                buffer.SetPixel(titleStartX - 1, 0, ' ', borderAttrs);
                for (int i = 0; i < renderTitleString.Length; i++)
                {
                    buffer.SetPixel(titleStartX + i, 0, renderTitleString[i], borderAttrs);
                }

                buffer.SetPixel(titleStartX + renderTitleString.Length, 0, ' ', borderAttrs);
            }
        }
    }

    private bool _closing;
    private bool _showClosingGlyph;

    private bool _moving;
    private int _movingStartX;
    private int _movingStartY;
    private Point _movingStartPoint;

    private bool _resizing;
    private int _resizingStartWidth;
    private int _resizingStartHeight;
    private Point _resizingStartPoint;

    public void Window_OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        // Moving is enabled only when windows is not resizing, and vice versa
        if (!_moving && !_resizing && !_closing)
        {
            Point point = args.GetPosition(this);
            Point parentPoint = args.GetPosition(GetWindowsHost());
            if (point.Y == 0 && point.X == 3)
            {
                _closing = true;
                _showClosingGlyph = true;
                ConsoleApplication.Instance.BeginCaptureInput(this);
                // closing is started, we should redraw the border
                Invalidate();
                args.Handled = true;
            }
            else if (point.Y == 0)
            {
                _moving = true;
                _movingStartPoint = parentPoint;
                _movingStartX = RenderSlotRect.TopLeft.X;
                _movingStartY = RenderSlotRect.TopLeft.Y;
                ConsoleApplication.Instance.BeginCaptureInput(this);
                // moving is started, we should redraw the border
                Invalidate();
                args.Handled = true;
            }
            else if (point.X == ActualWidth - 3 && point.Y == ActualHeight - 2)
            {
                _resizing = true;
                _resizingStartPoint = parentPoint;
                _resizingStartWidth = ActualWidth;
                _resizingStartHeight = ActualHeight;
                ConsoleApplication.Instance.BeginCaptureInput(this);
                // resizing is started, we should redraw the border
                Invalidate();
                args.Handled = true;
            }
        }
    }

    public void Close()
    {
        HandleClosing();
    }

    protected void HandleClosing()
    {
        var args = new CancelEventArgs(this, ClosingEvent);

        ConsoleApplication.Instance.EventManager.ProcessRoutedEvent(ClosingEvent, args);

        if (!args.Cancel)
        {
            GetWindowsHost()?.CloseWindow(this);
        }
    }

    public void Window_OnMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (_closing)
        {
            Point point = args.GetPosition(this);
            if (point.X == 3 && point.Y == 0)
            {
                HandleClosing();
            }

            _closing = false;
            _showClosingGlyph = false;
            ConsoleApplication.Instance.EndCaptureInput(this);
            Invalidate();
            args.Handled = true;
        }

        if (_moving)
        {
            _moving = false;
            ConsoleApplication.Instance.EndCaptureInput(this);
            Invalidate();
            args.Handled = true;
        }

        if (_resizing)
        {
            _resizing = false;
            ConsoleApplication.Instance.EndCaptureInput(this);
            Invalidate();
            args.Handled = true;
        }
    }

    public void Window_OnMouseMove(object sender, MouseEventArgs args)
    {
        if (_closing)
        {
            Point point = args.GetPosition(this);
            bool anyChanged = false;
            if (point.X == 3 && point.Y == 0)
            {
                if (!_showClosingGlyph)
                {
                    _showClosingGlyph = true;
                    anyChanged = true;
                }
            }
            else
            {
                if (_showClosingGlyph)
                {
                    _showClosingGlyph = false;
                    anyChanged = true;
                }
            }

            if (anyChanged)
                Invalidate();
            args.Handled = true;
        }

        if (_moving)
        {
            Point parentPoint = args.GetPosition(GetWindowsHost());
            Vector vector = new Vector(parentPoint.X - _movingStartPoint.X, parentPoint.Y - _movingStartPoint.Y);
            X = _movingStartX + vector.X;
            Y = _movingStartY + vector.Y;
            GetWindowsHost()?.Invalidate();
            args.Handled = true;
        }

        if (_resizing)
        {
            Point parentPoint = args.GetPosition(GetWindowsHost());
            int deltaWidth = parentPoint.X - _resizingStartPoint.X;
            int deltaHeight = parentPoint.Y - _resizingStartPoint.Y;
            int width = _resizingStartWidth + deltaWidth;
            int height = _resizingStartHeight + deltaHeight;
            bool anyChanged = false;
            if (width >= 4)
            {
                Width = width;
                anyChanged = true;
            }

            if (height >= 3)
            {
                Height = height;
                anyChanged = true;
            }

            if (anyChanged)
                Invalidate();
            args.Handled = true;
        }
    }

    public void Window_OnActivated(object? sender, EventArgs args)
    {
        Invalidate();
    }

    public void Window_OnDeactivated(object? sender, EventArgs args)
    {
        Invalidate();
    }

    public event CancelEventHandler Closing
    {
        add => AddHandler(ClosingEvent, value);
        remove => RemoveHandler(ClosingEvent, value);
    }

    public event EventHandler Closed
    {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }
}