using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

/// <summary>
/// Represents a control that a user can select and clear.
/// </summary>
public class CheckBox : ButtonBase
{
    public CheckBox()
    {
        OnClick += CheckBox_OnClick;
    }

    private void CheckBox_OnClick(object sender, RoutedEventArgs routedEventArgs)
    {
        Checked = !Checked;
    }

    private string? _caption;

    public string? Caption
    {
        get => _caption;
        set
        {
            if (_caption != value)
            {
                _caption = value;
                Invalidate();
            }
        }
    }

    private bool _isChecked;

    public bool Checked
    {
        get => _isChecked;
        set
        {
            if (_isChecked != value)
            {
                _isChecked = value;
                RaisePropertyChanged("Checked");
                Invalidate();
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!string.IsNullOrEmpty(_caption))
        {
            Size minButtonSize = new Size(_caption.Length + 4, 1);
            return minButtonSize;
        }

        return new Size(8, 1);
    }

    public override void Render(RenderingBuffer buffer)
    {
        Attr captionAttrs;
        if (HasFocus)
            captionAttrs = Colors.Blend(Color.White, Color.DarkGreen);
        else
            captionAttrs = Colors.Blend(Color.Black, Color.DarkGreen);

        Attr buttonAttrs = captionAttrs;
//            if ( pressed )
//                buttonAttrs = Colors.Blend(Color.Black, Color.DarkGreen);

        buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);

        buffer.SetPixel(0, 0, Pressed ? '<' : '[', buttonAttrs);
        buffer.SetPixel(1, 0, Checked ? 'X' : ' ', buttonAttrs);
        buffer.SetPixel(2, 0, Pressed ? '>' : ']', buttonAttrs);
        buffer.SetPixel(3, 0, ' ', buttonAttrs);
        if (null != _caption)
            RenderString(_caption, buffer, 4, 0, ActualWidth - 4, captionAttrs);
    }
}