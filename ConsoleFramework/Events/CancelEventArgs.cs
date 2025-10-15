namespace ConsoleFramework.Events;

public delegate void CancelEventHandler(object sender, CancelEventArgs e);

public class CancelEventArgs : RoutedEventArgs
{
    public CancelEventArgs(object source, RoutedEvent routedEvent) : base(source, routedEvent)
    {
    }

    public bool Cancel { get; set; }
}