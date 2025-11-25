namespace ConsoleFramework.Events;

public delegate void CancelEventHandler(object sender, CancelEventArgs e);

public class CancelEventArgs(object source, RoutedEvent routedEvent) : RoutedEventArgs(source, routedEvent)
{
    public bool Cancel { get; set; }
}