using System;

namespace ConsoleFramework.Core;

public struct Point : IEquatable<Point>
{
    public static bool operator ==(Point point1, Point point2)
    {
        return point1.X == point2.X && point1.Y == point2.Y;
    }

    public static bool operator !=(Point point1, Point point2)
    {
        return !(point1 == point2);
    }

    public static bool Equals(Point point1, Point point2)
    {
        return point1.X.Equals(point2.X) && point1.Y.Equals(point2.Y);
    }

    public override bool Equals(object? o)
    {
        if (o is not Point point)
        {
            return false;
        }

        return Equals(this, point);
    }

    public bool Equals(Point value)
    {
        return Equals(this, value);
    }

    public override int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }

    public int X { get; set; }

    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public void Offset(int offsetX, int offsetY)
    {
        X += offsetX;
        Y += offsetY;
    }

    public static Point operator +(Point point, Vector vector)
    {
        return new Point(point.X + vector.X, point.Y + vector.Y);
    }

    public static Point Add(Point point, Vector vector)
    {
        return new Point(point.X + vector.X, point.Y + vector.Y);
    }

    public static Point operator -(Point point, Vector vector)
    {
        return new Point(point.X - vector.X, point.Y - vector.Y);
    }

    public static Point Subtract(Point point, Vector vector)
    {
        return new Point(point.X - vector.X, point.Y - vector.Y);
    }

    public static Vector operator -(Point point1, Point point2)
    {
        return new Vector(point1.X - point2.X, point1.Y - point2.Y);
    }

    public static Vector Subtract(Point point1, Point point2)
    {
        return new Vector(point1.X - point2.X, point1.Y - point2.Y);
    }

    public static explicit operator Vector(Point point)
    {
        return new Vector(point.X, point.Y);
    }

    public override string ToString()
    {
        return $"{X};{Y}";
    }
}