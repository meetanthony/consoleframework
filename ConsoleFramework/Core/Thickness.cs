using System;
using ConsoleFramework.Xaml;

namespace ConsoleFramework.Core;

/// <summary>
/// WPF Thickness analog but using integers instead doubles.
/// </summary>
[TypeConverter(typeof(ThicknessConverter))]
public struct Thickness : IEquatable<Thickness>
{
    public int Left { get; set; }

    public int Top { get; set; }

    public int Right { get; set; }

    public int Bottom { get; set; }

    public Thickness(int uniformLength)
    {
        Left = Top = Right = Bottom = uniformLength;
    }

    public Thickness(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    internal bool IsZero()
    {
        return Left == 0 && Right == 0 && Top == 0 && Bottom == 0;
    }

    internal bool IsUniform()
    {
        return Left == Top && Left == Right && Left == Bottom;
    }

    public static bool operator ==(Thickness t1, Thickness t2)
    {
        return t1.Left == t2.Left && t1.Top == t2.Top && t1.Right == t2.Right && t1.Bottom == t2.Bottom;
    }

    public static bool operator !=(Thickness t1, Thickness t2)
    {
        return !(t1 == t2);
    }

    public bool Equals(Thickness other)
    {
        return other.Left == Left && other.Top == Top && other.Right == Right && other.Bottom == Bottom;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (obj.GetType() != typeof(Thickness)) return false;
        return Equals((Thickness)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int result = Left;
            result = (result * 397) ^ Top;
            result = (result * 397) ^ Right;
            result = (result * 397) ^ Bottom;
            return result;
        }
    }

    public override string ToString()
    {
        return string.Format("{0},{1},{2},{3}", Left, Top, Right, Bottom);
    }
}