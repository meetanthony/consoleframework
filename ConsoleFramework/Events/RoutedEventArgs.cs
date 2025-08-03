using System;

namespace ConsoleFramework.Events;

public delegate void RoutedEventHandler(object sender, RoutedEventArgs e);

public class RoutedEventArgs : EventArgs
{
    public bool Handled { get; set; }

    public object? Source { get; }

    public RoutedEvent RoutedEvent { get; }

    public RoutedEventArgs(object? source, RoutedEvent routedEvent)
    {
        Source = source;
        RoutedEvent = routedEvent;
    }
}