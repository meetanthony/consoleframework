using ConsoleFramework.Core;
using ConsoleFramework.Events;
using ConsoleFramework.Native;
using ConsoleFramework.Rendering;

namespace ConsoleFramework.Controls;

public class RadioGroup : Panel
{
    private int? _selectedItemIndex;

    public int? SelectedItemIndex
    {
        get => _selectedItemIndex;
        set
        {
            if (_selectedItemIndex != value)
            {
                _selectedItemIndex = value;
                RaisePropertyChanged(nameof(SelectedItemIndex));
                RaisePropertyChanged(nameof(SelectedItem));
            }
        }
    }

    public RadioButton? SelectedItem => _selectedItemIndex.HasValue ? (RadioButton)((Control)this).Children[_selectedItemIndex.Value] : null;

    public RadioGroup()
    {
        Children.ControlAdded += OnControlAdded;
        Children.ControlRemoved -= OnControlRemoved;
    }

    private void OnControlRemoved(Control control)
    {
        if (!(control is RadioButton)) return;
        var radioButton = (RadioButton)control;
        radioButton.OnClick -= radioButton_OnClick;
    }

    private void OnControlAdded(Control control)
    {
        if (!(control is RadioButton)) return;
        var radioButton = (RadioButton)control;
        radioButton.OnClick += radioButton_OnClick;
        int index = ((Control)this).Children.IndexOf(radioButton);
        radioButton.Checked = _selectedItemIndex != null && (_selectedItemIndex == index);
    }

    private void radioButton_OnClick(object sender, RoutedEventArgs args)
    {
        foreach (var child in Children)
        {
            if (child is RadioButton && child != sender)
            {
                ((RadioButton)child).Checked = false;
            }
        }

        ((RadioButton)sender).Checked = true;
        int index = ((Control)this).Children.IndexOf((Control)sender);
        SelectedItemIndex = index;
    }
}

public class RadioButton : CheckBox
{
    public override void Render(RenderingBuffer buffer)
    {
        var captionAttrs = Colors.Blend(
            HasFocus ? Color.White : Color.Black,
            Color.DarkGreen);

        Attr buttonAttrs = captionAttrs;
        //            if ( pressed )
        //                buttonAttrs = Colors.Blend(Color.Black, Color.DarkGreen);

        buffer.SetOpacityRect(0, 0, ActualWidth, ActualHeight, 3);

        buffer.SetPixel(0, 0, Pressed ? '<' : '(', buttonAttrs);
        buffer.SetPixel(1, 0, Checked ? 'X' : ' ', buttonAttrs);
        buffer.SetPixel(2, 0, Pressed ? '>' : ')', buttonAttrs);
        buffer.SetPixel(3, 0, ' ', buttonAttrs);
        if (null != Caption)
            RenderString(Caption, buffer, 4, 0, ActualWidth - 4, captionAttrs);
    }
}