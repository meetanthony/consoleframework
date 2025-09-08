using System;
using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// Event args containing info for ScrollViewer - how to display inner content.
/// </summary>
public class ContentShouldBeScrolledEventArgs : RoutedEventArgs
{
    private readonly int? _mostLeftVisibleX;
    private readonly int? _mostRightVisibleX;
    private readonly int? _mostTopVisibleY;
    private readonly int? _mostBottomVisibleY;

    public ContentShouldBeScrolledEventArgs(object source, RoutedEvent routedEvent,
        int? mostLeftVisibleX, int? mostRightVisibleX,
        int? mostTopVisibleY, int? mostBottomVisibleY)
        : base(source, routedEvent)
    {
        if (mostLeftVisibleX.HasValue && mostRightVisibleX.HasValue)
            throw new ArgumentException("Only one of X values can be specified");
        if (mostTopVisibleY.HasValue && mostBottomVisibleY.HasValue)
            throw new ArgumentException("Only one of Y values can be specified");
        _mostLeftVisibleX = mostLeftVisibleX;
        _mostRightVisibleX = mostRightVisibleX;
        _mostTopVisibleY = mostTopVisibleY;
        _mostBottomVisibleY = mostBottomVisibleY;
    }

    public int? MostLeftVisibleX => _mostLeftVisibleX;

    public int? MostRightVisibleX => _mostRightVisibleX;

    public int? MostTopVisibleY => _mostTopVisibleY;

    public int? MostBottomVisibleY => _mostBottomVisibleY;
}

public delegate void ContentShouldBeScrolledEventHandler(object sender,
    ContentShouldBeScrolledEventArgs args);

/// <summary>
/// Контрол, виртуализирующий содержимое так, что можно прокручивать его, если
/// оно не вмещается в отведённое пространство.
/// todo : add scroller dragging support
/// </summary>
public class ScrollViewer : Control
{
    /// <summary>
    /// Event can be fired by children when needs to explicitly set current
    /// visible region (for example, ListBox after mouse wheel scrolling).
    /// </summary>
    public static RoutedEvent ContentShouldBeScrolledEvent =
        EventManager.RegisterRoutedEvent("ContentShouldBeScrolled", RoutingStrategy.Bubble,
            typeof(ContentShouldBeScrolledEventHandler), typeof(ScrollViewer));

    public ScrollViewer()
    {
        AddHandler(MouseDownEvent, new MouseButtonEventHandler(OnMouseDown));
        HorizontalScrollEnabled = true;
        VerticalScrollEnabled = true;
        AddHandler(ContentShouldBeScrolledEvent, new ContentShouldBeScrolledEventHandler(OnContentShouldBeScrolled));
    }

    private void OnContentShouldBeScrolled(object sender, ContentShouldBeScrolledEventArgs args)
    {
        var renderSizeWidth = 0;
        var renderSizeHeight = 0;

        if (Content != null)
        {
            renderSizeWidth = Content.RenderSize.Width;
            renderSizeHeight = Content.RenderSize.Height;
        }
        
        if (args.MostLeftVisibleX.HasValue)
        {
            if (_deltaX <= args.MostLeftVisibleX.Value &&
                _deltaX + GetEffectiveWidth() > args.MostLeftVisibleX.Value)
            {
                // This X coord is already visible - do nothing
            }
            else
            {
                _deltaX = Math.Min(args.MostLeftVisibleX.Value,
                    renderSizeWidth - GetEffectiveWidth());
            }
        }
        else if (args.MostRightVisibleX.HasValue)
        {
            if (_deltaX <= args.MostRightVisibleX.Value &&
                _deltaX + GetEffectiveWidth() > args.MostRightVisibleX.Value)
            {
                // This X coord is already visible - do nothing
            }
            else
            {
                _deltaX = Math.Max(args.MostRightVisibleX.Value - GetEffectiveWidth() + 1,
                    0);
            }
        }

        if (args.MostTopVisibleY.HasValue)
        {
            if (_deltaY <= args.MostTopVisibleY.Value &&
                _deltaY + GetEffectiveHeight() > args.MostTopVisibleY.Value)
            {
                // This Y coord is already visible - do nothing
            }
            else
            {
                _deltaY = Math.Min(args.MostTopVisibleY.Value,
                    renderSizeHeight - GetEffectiveHeight());
            }
        }
        else if (args.MostBottomVisibleY.HasValue)
        {
            if (_deltaY <= args.MostBottomVisibleY.Value &&
                _deltaY + GetEffectiveHeight() > args.MostBottomVisibleY.Value)
            {
                // This Y coord is already visible - do nothing
            }
            else
            {
                _deltaY = Math.Max(args.MostBottomVisibleY.Value - GetEffectiveHeight() + 1,
                    0);
            }
        }

        Invalidate();
    }

    private int GetEffectiveWidth()
    {
        return VerticalScrollVisible ? RenderSize.Width - 1 : RenderSize.Width;
    }

    private int GetEffectiveHeight()
    {
        return HorizontalScrollVisible ? RenderSize.Height - 1 : RenderSize.Height;
    }

    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    public bool ContentFullyVisible => !_verticalScrollVisible && !_horizontalScrollVisible;

    public void ScrollContent(Direction direction, int delta)
    {
        var renderSizeWidth = 0;
        var renderSizeHeight = 0;

        if (Content != null)
        {
            renderSizeWidth = Content.RenderSize.Width;
            renderSizeHeight = Content.RenderSize.Height;
        }
        
        for (int i = 0; i < delta; i++)
            switch (direction)
            {
                case Direction.Left:
                {
                    // сколько места сейчас оставлено дочернему контролу
                    int remainingWidth = ActualWidth - (_verticalScrollVisible ? 1 : 0);
                    if (_deltaX < renderSizeWidth - remainingWidth)
                    {
                        _deltaX++;
                    }

                    Invalidate();
                    break;
                }
                case Direction.Right:
                {
                    if (_deltaX > 0)
                    {
                        _deltaX--;
                    }

                    Invalidate();
                    break;
                }
                case Direction.Up:
                {
                    // сколько места сейчас оставлено дочернему контролу
                    int remainingHeight = ActualHeight - (_horizontalScrollVisible ? 1 : 0);
                    if (_deltaY < renderSizeHeight - remainingHeight)
                    {
                        _deltaY++;
                    }

                    Invalidate();
                    break;
                }
                case Direction.Down:
                {
                    if (_deltaY > 0)
                    {
                        _deltaY--;
                    }

                    Invalidate();
                    break;
                }
            }
    }

    public int DeltaX => _deltaX;

    public int DeltaY => _deltaY;

    public bool HorizontalScrollVisible => _horizontalScrollVisible;

    public bool VerticalScrollVisible => _verticalScrollVisible;

    public bool VerticalScrollEnabled { get; set; }

    public bool HorizontalScrollEnabled { get; set; }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        var renderSizeWidth = 0;
        var renderSizeHeight = 0;

        if (Content != null)
        {
            renderSizeWidth = Content.RenderSize.Width;
            renderSizeHeight = Content.RenderSize.Height;
        }
        
        Point pos = args.GetPosition(this);
        if (_horizontalScrollVisible)
        {
            Point leftArrowPos = new Point(0, ActualHeight - 1);
            Point rightArrowPos = new Point(ActualWidth - (1 + (_verticalScrollVisible ? 1 : 0)),
                ActualHeight - 1);
            if (leftArrowPos == pos)
            {
                if (_deltaX > 0)
                {
                    _deltaX--;
                    Invalidate();
                }
            }
            else if (rightArrowPos == pos)
            {
                // сколько места сейчас оставлено дочернему контролу
                int remainingWidth = ActualWidth - (_verticalScrollVisible ? 1 : 0);
                if (_deltaX < renderSizeWidth - remainingWidth)
                {
                    _deltaX++;
                    Invalidate();
                }
            }
            else if (pos.Y == ActualHeight - 1)
            {
                // Clicked somewhere in scrollbar
                Point? horizontalScrollerPos = HorizontalScrollerPos;
                if (horizontalScrollerPos.HasValue)
                {
                    int remainingWidth = ActualWidth - (_verticalScrollVisible ? 1 : 0);
                    int itemsPerScrollerPos = renderSizeWidth / remainingWidth;
                    if (pos.X < horizontalScrollerPos.Value.X)
                    {
                        _deltaX = Math.Max(0, _deltaX - itemsPerScrollerPos);
                        Invalidate();
                    }
                    else if (pos.X > horizontalScrollerPos.Value.X)
                    {
                        _deltaX = Math.Min(renderSizeWidth - remainingWidth,
                            _deltaX + itemsPerScrollerPos);
                        Invalidate();
                    }
                    else
                    {
                        // Click on scroller
                        // todo : make scroller draggable
                    }
                }
            }
        }

        if (_verticalScrollVisible)
        {
            Point upArrowPos = new Point(ActualWidth - 1, 0);
            Point downArrowPos = new Point(ActualWidth - 1,
                ActualHeight - (1 + (_horizontalScrollVisible ? 1 : 0)));
            if (pos == upArrowPos)
            {
                if (_deltaY > 0)
                {
                    _deltaY--;
                    Invalidate();
                }
            }
            else if (pos == downArrowPos)
            {
                // сколько места сейчас оставлено дочернему контролу
                int remainingHeight = ActualHeight - (_horizontalScrollVisible ? 1 : 0);
                if (_deltaY < renderSizeHeight - remainingHeight)
                {
                    _deltaY++;
                    Invalidate();
                }
            }
            else if (pos.X == ActualWidth - 1)
            {
                // Clicked somewhere in scrollbar
                Point? verticalScrollerPos = VerticalScrollerPos;
                if (verticalScrollerPos.HasValue)
                {
                    int remainingHeight = ActualHeight - (_horizontalScrollVisible ? 1 : 0);
                    int itemsPerScrollerPos = renderSizeHeight / remainingHeight;
                    if (pos.Y < verticalScrollerPos.Value.Y)
                    {
                        _deltaY = Math.Max(0, _deltaY - itemsPerScrollerPos);
                        Invalidate();
                    }
                    else if (pos.Y > verticalScrollerPos.Value.Y)
                    {
                        _deltaY = Math.Min(renderSizeHeight - remainingHeight,
                            _deltaY + itemsPerScrollerPos);
                        Invalidate();
                    }
                    else
                    {
                        // Click on scroller
                        // todo : make scroller draggable
                    }
                }
            }
        }

        args.Handled = true;
    }

    private Control? _content;

    public Control? Content
    {
        get => _content;
        set
        {
            if (_content != null)
                RemoveChild(_content);
            if (value != null)
                AddChild(value);
            _content = value;
        }
    }

    private bool _horizontalScrollVisible;
    private bool _verticalScrollVisible;

    private int _deltaX;
    private int _deltaY;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Content == null) return new Size(0, 0);

        // Размещаем контрол так, как будто бы у него имеется сколько угодно пространства
        Content.Measure(new Size(int.MaxValue, int.MaxValue));

        Size desiredSize = Content.DesiredSize;

        _horizontalScrollVisible = HorizontalScrollEnabled && (desiredSize.Width > availableSize.Width);
        _verticalScrollVisible = VerticalScrollEnabled && (desiredSize.Height > availableSize.Height);

        int width = Math.Min(_verticalScrollVisible ? desiredSize.Width + 1 : desiredSize.Width, availableSize.Width);
        int height = Math.Min(_horizontalScrollVisible ? desiredSize.Height + 1 : desiredSize.Height,
            availableSize.Height);

        // Если горизонтальная прокрутка отключена - то мы должны сообщить контролу, что по горизонтали он будет иметь не int.MaxValue
        // пространства, а ровно width. Таким образом мы даём возможность контролу приспособиться к тому, что прокрутки по горизонтали не будет.
        // Аналогично и с вертикальной прокруткой. Так как последний вызов Measure должен быть именно с такими размерами, которые реально
        // будут использоваться при размещении, то мы и должны выполнить Measure ещё раз.
        if (!HorizontalScrollEnabled || !VerticalScrollEnabled)
        {
            Content.Measure(new Size(HorizontalScrollEnabled ? int.MaxValue : width,
                VerticalScrollEnabled ? int.MaxValue : height));
        }

        Size result = new Size(width, height);
        return result;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Content == null) return finalSize;
        int width = finalSize.Width;
        int height = finalSize.Height;
        Rect finalRect = new Rect(new Point(-_deltaX, -_deltaY),
            new Size(
                _deltaX + Math.Max(0, _verticalScrollVisible ? width - 1 : width),
                _deltaY + Math.Max(0, _horizontalScrollVisible ? height - 1 : height))
        );

        // если мы сдвинули окно просмотра, а потом размеры, доступные контролу, увеличились,
        // мы должны вернуть дочерний контрол в точку (0, 0)
        if (_deltaX > Content.DesiredSize.Width - Math.Max(0, _verticalScrollVisible ? width - 1 : width))
        {
            _deltaX = 0;
            finalRect = new Rect(new Point(-_deltaX, -_deltaY),
                new Size(
                    _deltaX + Math.Max(0, _verticalScrollVisible ? width - 1 : width),
                    _deltaY + Math.Max(0, _horizontalScrollVisible ? height - 1 : height))
            );
        }

        if (_deltaY > Content.DesiredSize.Height - Math.Max(0, _horizontalScrollVisible ? height - 1 : height))
        {
            _deltaY = 0;
            finalRect = new Rect(new Point(-_deltaX, -_deltaY),
                new Size(
                    _deltaX + Math.Max(0, _verticalScrollVisible ? width - 1 : width),
                    _deltaY + Math.Max(0, _horizontalScrollVisible ? height - 1 : height))
            );
        }

        Content.Arrange(finalRect);
        int resultWidth =
            Math.Min(_verticalScrollVisible ? 1 + finalRect.Width : finalRect.Width, width);
        int resultHeight =
            Math.Min(_horizontalScrollVisible ? 1 + finalRect.Height : finalRect.Height, height);

        Size result = new Size(resultWidth, resultHeight);
        return result;
    }

    /// <summary>
    /// Returns position of horizontal scroller if it is visible now, null otherwise.
    /// </summary>
    public Point? HorizontalScrollerPos
    {
        get
        {
            if (!_horizontalScrollVisible) return null;

            var renderSizeWidth = 0;

            if (Content != null)
            {
                renderSizeWidth = Content.RenderSize.Width;
            }
            
            if (ActualWidth > 3 + (_verticalScrollVisible ? 1 : 0))
            {
                int remainingWidth = ActualWidth - (_verticalScrollVisible ? 1 : 0);
                int extraWidth = renderSizeWidth - remainingWidth;
                int pages = extraWidth / (remainingWidth - 2 - 1);

                // Relative to scrollbar (without arrows)
                int scrollerPos;
                if (pages == 0)
                {
                    double posInDelta = (remainingWidth * 1.0 - 2 - 1) / extraWidth;
                    scrollerPos = (int)Math.Round(posInDelta * _deltaX);
                }
                else
                {
                    double deltaInPos = (extraWidth * 1.0) / (remainingWidth - 2 - 1);
                    scrollerPos = (int)Math.Round(_deltaX / (deltaInPos));
                }

                // Relative to whole control
                return new Point(scrollerPos + 1, ActualHeight - 1);
            }

            return null;
        }
    }

    /// <summary>
    /// Returns position of scroller if it is visible now, null otherwise.
    /// </summary>
    public Point? VerticalScrollerPos
    {
        get
        {
            if (!_verticalScrollVisible) return null;

            var renderSizeHeight = 0;

            if (Content != null)
            {
                renderSizeHeight = Content.RenderSize.Height;
            }
            
            if (ActualHeight > 3 + (_horizontalScrollVisible ? 1 : 0))
            {
                int remainingHeight = ActualHeight - (_horizontalScrollVisible ? 1 : 0);
                int extraHeight = renderSizeHeight - remainingHeight;
                int pages = extraHeight / (remainingHeight - 2 - 1);

                // Relative to scrollbar (without arrows)
                int scrollerPos;
                if (pages == 0)
                {
                    double posInDelta = (remainingHeight * 1.0 - 2 - 1) / extraHeight;
                    scrollerPos = (int)Math.Round(posInDelta * _deltaY);
                }
                else
                {
                    double deltaInPos = (extraHeight * 1.0) / (remainingHeight - 2 - 1);
                    scrollerPos = (int)Math.Round(_deltaY / (deltaInPos));
                }

                // Relative to whole control
                return new Point(ActualWidth - 1, scrollerPos + 1);
            }

            return null;
        }
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.DarkCyan, Color.DarkBlue);

        buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 2);

        var renderSizeWidth = 0;
        var renderSizeHeight = 0;

        if (Content != null)
        {
            renderSizeWidth = Content.RenderSize.Width;
            renderSizeHeight = Content.RenderSize.Height;
        }
        
        if (_horizontalScrollVisible)
        {
            buffer.SetOpacityRect(0, ActualHeight - 1, ActualWidth, 1, 0);
            buffer.SetPixel(0, ActualHeight - 1, UnicodeTable.ArrowLeft, attr); // ◄
            // оставляем дополнительный пиксель справа, если одновременно видны оба скроллбара
            int rightOffset = _verticalScrollVisible ? 1 : 0;
            if (ActualWidth > 2 + rightOffset)
            {
                buffer.FillRectangle(1, ActualHeight - 1, ActualWidth - (2 + rightOffset), 1,
                    UnicodeTable.MediumShade, attr); // ▒
            }

            if (ActualWidth > 1 + rightOffset)
            {
                buffer.SetPixel(ActualWidth - (1 + rightOffset), ActualHeight - 1,
                    UnicodeTable.ArrowRight, attr); // ►
            }

            // определим, в каком месте находится ползунок
            if (ActualWidth > 3 + (_verticalScrollVisible ? 1 : 0))
            {
                int remainingWidth = ActualWidth - (_verticalScrollVisible ? 1 : 0);
                int extraWidth = renderSizeWidth - remainingWidth;
                int pages = extraWidth / (remainingWidth - 2 - 1);

                //Debugger.Log( 1, "", "pages: " + pages + "\n" );

                int scrollerPos;
                if (pages == 0)
                {
                    double posInDelta = (remainingWidth * 1.0 - 2 - 1) / extraWidth;
                    //Debugger.Log( 1, "", "posInDelta: " + posInDelta + "\n" );
                    scrollerPos = (int)Math.Round(posInDelta * _deltaX);
                }
                else
                {
                    double deltaInPos = (extraWidth * 1.0) / (remainingWidth - 2 - 1);
                    //Debugger.Log( 1, "", "deltaX/( deltaInPos ): " + deltaX/( deltaInPos ) + "\n" );
                    scrollerPos = (int)Math.Round(_deltaX / (deltaInPos));
                }

                buffer.SetPixel(1 + scrollerPos, ActualHeight - 1, UnicodeTable.BlackSquare, attr); // ■
            }
            else if (ActualWidth == 3 + (_verticalScrollVisible ? 1 : 0))
            {
                buffer.SetPixel(1, ActualHeight - 1, UnicodeTable.BlackSquare, attr); // ■
            }
        }

        if (_verticalScrollVisible)
        {
            buffer.SetOpacityRect(ActualWidth - 1, 0, 1, ActualHeight, 0);

            buffer.SetPixel(ActualWidth - 1, 0, UnicodeTable.ArrowUp, attr); // ▲
            // оставляем дополнительный пиксель снизу, если одновременно видны оба скроллбара
            int downOffset = _horizontalScrollVisible ? 1 : 0;
            if (ActualHeight > 2 + downOffset)
            {
                buffer.FillRectangle(ActualWidth - 1, 1, 1, ActualHeight - (2 + downOffset), UnicodeTable.MediumShade,
                    attr); // ▒
            }

            if (ActualHeight > 1 + downOffset)
            {
                buffer.SetPixel(ActualWidth - 1, ActualHeight - (1 + downOffset), UnicodeTable.ArrowDown, attr); // ▼
            }

            // определим, в каком месте находится ползунок
            if (ActualHeight > 3 + (_horizontalScrollVisible ? 1 : 0))
            {
                int remainingHeight = ActualHeight - (_horizontalScrollVisible ? 1 : 0);
                int extraHeight = renderSizeHeight - remainingHeight;
                int pages = extraHeight / (remainingHeight - 2 - 1);

                //Debugger.Log( 1, "", "pages: " + pages + "\n" );

                int scrollerPos;
                if (pages == 0)
                {
                    double posInDelta = (remainingHeight * 1.0 - 2 - 1) / extraHeight;
                    //Debugger.Log( 1, "", "posInDelta: " + posInDelta + "\n" );
                    scrollerPos = (int)Math.Round(posInDelta * _deltaY);
                }
                else
                {
                    double deltaInPos = (extraHeight * 1.0) / (remainingHeight - 2 - 1);
                    //Debugger.Log( 1, "", "deltaY/( deltaInPos ): " + deltaY/( deltaInPos ) + "\n" );
                    scrollerPos = (int)Math.Round(_deltaY / (deltaInPos));
                }

                buffer.SetPixel(ActualWidth - 1, 1 + scrollerPos, UnicodeTable.BlackSquare, attr); // ■
            }
            else if (ActualHeight == 3 + (_horizontalScrollVisible ? 1 : 0))
            {
                buffer.SetPixel(ActualWidth - 1, 1, UnicodeTable.BlackSquare, attr); // ■
            }
        }

        if (_horizontalScrollVisible && _verticalScrollVisible)
        {
            buffer.SetPixel(ActualWidth - 1, ActualHeight - 1, UnicodeTable.SingleFrameBottomRightCorner, attr); // ┘
        }
    }
}