using System;

namespace ConsoleFramework.Core;

public struct Vector
{
    public override string ToString()
    {
        return string.Format("{0};{1}", X, Y);
    }

    public static bool operator ==(Vector vector1, Vector vector2)
    {
        return ((vector1.X == vector2.X) && (vector1.Y == vector2.Y));
    }

    public static bool operator !=(Vector vector1, Vector vector2)
    {
        return !(vector1 == vector2);
    }

    public static bool Equals(Vector vector1, Vector vector2)
    {
        return (vector1.X.Equals(vector2.X) && vector1.Y.Equals(vector2.Y));
    }

    public override bool Equals(object? o)
    {
        if (o is not Vector vector)
        {
            return false;
        }

        return Equals(this, vector);
    }

    public bool Equals(Vector value)
    {
        return Equals(this, value);
    }

    public override int GetHashCode()
    {
        return (X.GetHashCode() ^ Y.GetHashCode());
    }

    public int X { get; set; }

    public int Y { get; set; }

    public Vector(int x, int y)
    {
        X = x;
        Y = y;
    }

    public double Length => Math.Sqrt((X * X) + (Y * Y));

    public double LengthSquared => ((X * X) + (Y * Y));

    public static double CrossProduct(Vector vector1, Vector vector2)
    {
        return ((vector1.X * vector2.Y) - (vector1.Y * vector2.X));
    }

    public static double AngleBetween(Vector vector1, Vector vector2)
    {
        double y = (vector1.X * vector2.Y) - (vector2.X * vector1.Y);
        double x = (vector1.X * vector2.X) + (vector1.Y * vector2.Y);
        return (Math.Atan2(y, x) * 57.295779513082323);
    }

    public static Vector operator -(Vector vector)
    {
        return new Vector(-vector.X, -vector.Y);
    }

    public void Negate()
    {
        X = -X;
        Y = -Y;
    }

    public static Vector operator +(Vector vector1, Vector vector2)
    {
        return new Vector(vector1.X + vector2.X, vector1.Y + vector2.Y);
    }

    public static Vector Add(Vector vector1, Vector vector2)
    {
        return new Vector(vector1.X + vector2.X, vector1.Y + vector2.Y);
    }

    public static Vector operator -(Vector vector1, Vector vector2)
    {
        return new Vector(vector1.X - vector2.X, vector1.Y - vector2.Y);
    }

    public static Vector Subtract(Vector vector1, Vector vector2)
    {
        return new Vector(vector1.X - vector2.X, vector1.Y - vector2.Y);
    }

    public static Point operator +(Vector vector, Point point)
    {
        return new Point(point.X + vector.X, point.Y + vector.Y);
    }

    public static Point Add(Vector vector, Point point)
    {
        return new Point(point.X + vector.X, point.Y + vector.Y);
    }

    public static Vector operator *(Vector vector, int scalar)
    {
        return new Vector(vector.X * scalar, vector.Y * scalar);
    }

    public static Vector Multiply(Vector vector, int scalar)
    {
        return new Vector(vector.X * scalar, vector.Y * scalar);
    }

    public static Vector operator *(int scalar, Vector vector)
    {
        return new Vector(vector.X * scalar, vector.Y * scalar);
    }

    public static Vector Multiply(int scalar, Vector vector)
    {
        return new Vector(vector.X * scalar, vector.Y * scalar);
    }

    public static double operator *(Vector vector1, Vector vector2)
    {
        return ((vector1.X * vector2.X) + (vector1.Y * vector2.Y));
    }

    public static double Multiply(Vector vector1, Vector vector2)
    {
        return ((vector1.X * vector2.X) + (vector1.Y * vector2.Y));
    }

    public static double Determinant(Vector vector1, Vector vector2)
    {
        return ((vector1.X * vector2.Y) - (vector1.Y * vector2.X));
    }

    public static explicit operator Point(Vector vector)
    {
        return new Point(vector.X, vector.Y);
    }
}