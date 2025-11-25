using ConsoleFramework;
using ConsoleFramework.Controls;
using ConsoleFramework.Core;
using System.Diagnostics;
using ConsoleFramework.Binding.Validators;

namespace ManyControls;

public class ProgramFromCode : IProgram
{
    public void Main(string[] args)
    {
        using ConsoleApplication application = ConsoleApplication.Instance;

        WindowsHost windowsHost = new WindowsHost()
        {
            Name = "WindowsHost"
        };

        var windowX = 1;
        var windowY = 1;

        void ShowWindow(Window window)
        {
            window.X = windowX;
            windowX += window.Width ?? 5 + 1;

            window.Y = windowY;

            windowsHost.Show(window);
        }

        Window helloWorldWindow = CreateHelloWorldWindow();
        ShowWindow(helloWorldWindow);
        
        Window windowWithPanelAndControls = CreateWindowWithPanelAndControls();
        ShowWindow(windowWithPanelAndControls);

        application.Run(windowsHost);
    }

    private Window CreateHelloWorldWindow()
    {
        Window window = new Window
        {
            Height = 10,
            Width = 30,
            Name = "HelloWorldWindow",
            Title = "HelloWorldWindow"
        };

        TextBlock textBlock = new TextBlock
        {
            Text = "Hello, World!",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        window.Content = textBlock;

        return window;
    }

    private Window CreateWindowWithPanelAndControls()
    {
        const bool comboBoxExample = true;
        const bool listBoxExample = true;
        const bool groupBoxExample = true;

        var window = new Window
        {
            Height = 10,
            Width = 50,
            Name = "WindowWithPanelAndControls",
            Title = "WindowWithPanelAndControls"
        };

        var rootPanel = new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Orientation = Orientation.Vertical
        };

        if (comboBoxExample)
        {
            var panel = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Horizontal
            };

            // TextBlock
            var text = "ComboBox:";
            var textBlock = new TextBlock
            {
                Text = text,
                Width = text.Length,
                Margin = new Thickness(1)
            };
            panel.Children.Add(textBlock);

            // ComboBox with Items
            var comboBox = new ComboBox()
            {
                Margin = new Thickness(1)
            };
            for (int i = 0; i < 5; i++)
            {
                comboBox.Items.Add($"ComboBox item #{i}");
            }

            panel.Children.Add(comboBox);

            rootPanel.Children.Add(panel);
        }

        if (listBoxExample)
        {
            var panel = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Horizontal
            };

            // TextBlock
            var text = "ListBox:";
            var textBlock = new TextBlock
            {
                Text = text,
                Width = text.Length,
                Margin = new Thickness(1)
            };
            panel.Children.Add(textBlock);

            // ListBox with Items
            var listBox = new ListBox();
            for (int i = 0; i < 50; i++)
            {
                listBox.Items.Add($"ListBox item #{i}");
            }

            ScrollViewer scrollViewer = new ScrollViewer()
            {
                Margin = new Thickness(1),
                MaxHeight = 4
            };
            scrollViewer.Content = listBox;

            panel.Children.Add(scrollViewer);

            rootPanel.Children.Add(panel);
        }

        if (listBoxExample)
        {
            var groupBox = new GroupBox()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            var panel = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Orientation = Orientation.Horizontal
            };
            groupBox.Content = panel;

            // TextBlock
            var text = "Buttons in GroupBox:";
            var textBlock = new TextBlock
            {
                Text = text,
                Width = text.Length,
                Margin = new Thickness(1)
            };
            panel.Children.Add(textBlock);

            // Buttons
            for (int i = 0; i < 5; i++)
            {
                var button = new Button()
                {
                    Caption = $"Button #{i}",
                    Margin = new Thickness(1)
                };

                button.OnClick += (sender, _) => { ((Button)sender).Caption = "Clicked";};

                panel.Children.Add(button);
            }

            rootPanel.Children.Add(groupBox);
        }

        window.Content = rootPanel;

        return window;
    }
}