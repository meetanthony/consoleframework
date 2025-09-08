using System;

namespace ConsoleFramework;

public class TerminalSizeChangedEventArgs : EventArgs
{
    public readonly int Width;
    public readonly int Height;

    public TerminalSizeChangedEventArgs(int width, int height)
    {
        Width = width;
        Height = height;
    }
}