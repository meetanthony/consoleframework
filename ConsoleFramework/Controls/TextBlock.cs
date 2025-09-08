using ConsoleFramework.Core;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.Controls;

[ContentProperty(nameof(Text))]
public class TextBlock : Control
{
    private string? _text;

    private void Initialize()
    {
    }

    public TextBlock()
    {
        Initialize();
    }

    private Color _color = Color.Black;

    public Color Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                Invalidate();
            }
        }
    }

    public string? Text
    {
        get => _text;
        set
        {
            if (_text != value)
            {
                _text = value;
                Invalidate();
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (null != _text)
            return new Size(_text.Length, 1);
        return new Size(0, 0);
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr attr = Colors.Blend(_color, Color.DarkYellow);
        buffer.FillRectangle(0, 0, ActualWidth, ActualHeight, ' ', attr);
        for (int x = 0; x < ActualWidth; ++x)
        {
            for (int y = 0; y < ActualHeight; ++y)
            {
                if (_text != null && y == 0 && x < _text.Length)
                {
                    buffer.SetPixel(x, y, _text[x], attr);
                }
            }
        }

        buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);
    }

    public override string ToString()
    {
        return "TextBlock";
    }
}