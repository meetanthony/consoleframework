using System;
using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// Оборачивает указанный контрол рамкой с заголовком.
/// </summary>
public class GroupBox : Control
{
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

    private Control? _content;

    public Control? Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                if (_content != null)
                    RemoveChild(_content);

                _content = value;
                
                if (_content != null)
                    AddChild(_content);

                Invalidate();
            }
        }
    }

    private bool _fitSizeToContent;

    /// <summary>
    /// If true, child will always win in layout battle.
    /// If false (default), parent will always win (and groupbox size
    /// will be constrained according to passed availableSize).
    /// </summary>
    public bool FitSizeToContent
    {
        get => _fitSizeToContent;
        set
        {
            if (_fitSizeToContent != value)
            {
                _fitSizeToContent = value;
                Invalidate();
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Size contentSize = Size.Empty;
        if (_content != null)
        {
            _content.Measure(new Size(int.MaxValue, int.MaxValue));
            contentSize = _content.DesiredSize;
        }

        Size needSize = new Size(
            Math.Max(contentSize.Width + 2, _title.Length + 4),
            contentSize.Height + 2
        );
        if (_fitSizeToContent) return needSize;

        Size constrainedSize = new Size(
            Math.Min(needSize.Width, availableSize.Width),
            Math.Min(needSize.Height, availableSize.Height)
        );
        if (needSize != constrainedSize && _content != null)
        {
            // если контрол вместе с содержимым не помещается в availableSize,
            // то мы оставляем содержимому меньше места, чем ему хотелось бы,
            // и поэтому повторным вызовом Measure должны установить его реальные размеры,
            // которые будут использованы при размещении
            _content.Measure(new Size(
                Math.Max(0, constrainedSize.Width - 2),
                Math.Max(0, constrainedSize.Height - 2)
            ));
        }

        return constrainedSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (null == _content)
            return finalSize;
        Rect contentRect = new Rect(1, 1,
            Math.Max(0, finalSize.Width - 2),
            Math.Max(0, finalSize.Height - 2));
        _content.Arrange(contentRect);
        return finalSize;
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(Color.Black, Color.DarkGreen);

        // прозрачный фон для рамки
        buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);
        // полностью прозрачный внутри
        if (ActualWidth > 2 && ActualHeight > 2)
            buffer.SetOpacityRect(1, 1, ActualWidth - 2, ActualHeight - 2, 2);
        // title
        int titleRenderedWidth = 0;
        if (!string.IsNullOrEmpty(_title))
            titleRenderedWidth = RenderString(_title, buffer, 2, 0, ActualWidth - 4, attr);
        // upper border
        for (int x = 0; x < ActualWidth; x++)
        {
            char? c = null;
            if (x == 0)
                c = UnicodeTable.SingleFrameTopLeftCorner;
            else if (x == ActualWidth - 1)
                c = UnicodeTable.SingleFrameTopRightCorner;
            else if (x == 1 || x == 2 + titleRenderedWidth)
                c = ' ';
            else if (x > 2 + titleRenderedWidth && x < ActualWidth - 1)
                c = UnicodeTable.SingleFrameHorizontal;
            if (c != null)
                buffer.SetPixel(x, 0, c.Value, attr);
        }

        // left border
        if (ActualHeight > 2)
            buffer.FillRectangle(0, 1, 1, ActualHeight - 2, UnicodeTable.SingleFrameVertical, attr);
        if (ActualHeight > 1)
            buffer.SetPixel(0, ActualHeight - 1, UnicodeTable.SingleFrameBottomLeftCorner, attr);
        // right border
        if (ActualWidth > 1)
        {
            if (ActualHeight > 2)
                buffer.FillRectangle(ActualWidth - 1, 1, 1, ActualHeight - 2, UnicodeTable.SingleFrameVertical, attr);
            if (ActualHeight > 1)
                buffer.SetPixel(ActualWidth - 1, ActualHeight - 1, UnicodeTable.SingleFrameBottomRightCorner, attr);
        }

        // bottom border
        if (ActualHeight > 1 && ActualWidth > 2)
        {
            buffer.FillRectangle(1, ActualHeight - 1, ActualWidth - 2, 1, UnicodeTable.SingleFrameHorizontal, attr);
        }
    }
}