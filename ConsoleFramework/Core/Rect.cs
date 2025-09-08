using System;

namespace ConsoleFramework.Core;

public struct Rect : IFormattable
{
    private int x;
    private int y;
    private int width;
    private int height;
    private static readonly Rect SEmpty;

    public static bool operator ==(Rect rect1, Rect rect2)
    {
        return rect1.X == rect2.X
               && rect1.Y == rect2.Y
               && rect1.Width == rect2.Width
               && rect1.Height == rect2.Height;
    }

    public static bool operator !=(Rect rect1, Rect rect2)
    {
        return !(rect1 == rect2);
    }

    public static bool Equals(Rect rect1, Rect rect2)
    {
        if (rect1.IsEmpty)
        {
            return rect2.IsEmpty;
        }

        return rect1.X.Equals(rect2.X) && rect1.Y.Equals(rect2.Y) && rect1.Width.Equals(rect2.Width) &&
               rect1.Height.Equals(rect2.Height);
    }

    public override bool Equals(object? o)
    {
        if (o is not Rect rect)
        {
            return false;
        }

        return Equals(this, rect);
    }

    public bool Equals(Rect value)
    {
        return Equals(this, value);
    }

    public override int GetHashCode()
    {
        if (IsEmpty)
        {
            return 0;
        }

        return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^
               Height.GetHashCode();
    }

    public override string ToString()
    {
        return ConvertToString(null, null);
    }

    public string ToString(IFormatProvider provider)
    {
        return ConvertToString(null, provider);
    }

    string IFormattable.ToString(string? format, IFormatProvider? provider)
    {
        return ConvertToString(format, provider);
    }

    internal string ConvertToString(string? format, IFormatProvider? provider)
    {
        if (IsEmpty)
        {
            return "Empty";
        }

        const char numericListSeparator = ',';
        return string.Format(provider,
            "{1:" + format + "}{0}{2:" + format + "}{0}{3:" + format + "}{0}{4:" + format + "}",
            numericListSeparator, x, y, width, height);
    }

    public Rect(Rect copy)
    {
        x = copy.x;
        y = copy.y;
        width = copy.width;
        height = copy.height;
    }

    public Rect(Point location, Size size)
    {
        if (size.IsEmpty)
        {
            this = SEmpty;
        }
        else
        {
            x = location.X;
            y = location.Y;
            width = size.width;
            height = size.height;
        }
    }

    public Rect(int x, int y, int width, int height)
    {
        if (width < 0 || height < 0)
        {
            throw new ArgumentException("Size_WidthAndHeightCannotBeNegative");
        }

        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public Rect(Point point1, Point point2)
    {
        x = Math.Min(point1.X, point2.X);
        y = Math.Min(point1.Y, point2.Y);
        width = Math.Max(Math.Max(point1.X, point2.X) - x, 0);
        height = Math.Max(Math.Max(point1.Y, point2.Y) - y, 0);
    }

    public Rect(Point point, Vector vector) : this(point, point + vector)
    {
    }

    public Rect(Size size)
    {
        if (size.IsEmpty)
        {
            this = SEmpty;
        }
        else
        {
            x = y = 0;
            width = size.Width;
            height = size.Height;
        }
    }

    public static Rect Empty => SEmpty;

    public bool IsEmpty => width == 0 || height == 0;

    public Point Location
    {
        get => new(x, y);
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            x = value.X;
            y = value.Y;
        }
    }

    public Size Size
    {
        get
        {
            if (IsEmpty)
            {
                return Size.Empty;
            }

            return new Size(width, height);
        }
        set
        {
            if (value.IsEmpty)
            {
                this = SEmpty;
            }
            else
            {
                if (IsEmpty)
                {
                    throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
                }

                width = value.width;
                height = value.height;
            }
        }
    }

    public int X
    {
        get => x;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            x = value;
        }
    }

    public int Y
    {
        get => y;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            y = value;
        }
    }

    public int Width
    {
        get => width;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            if (value < 0)
            {
                throw new ArgumentException("Size_WidthCannotBeNegative");
            }

            width = value;
        }
    }

    public int Height
    {
        get => height;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            if (value < 0)
            {
                throw new ArgumentException("Size_HeightCannotBeNegative");
            }

            height = value;
        }
    }

    public int Left => x;

    public int Top => y;

    public int Right
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }

            return x + width;
        }
    }

    public int Bottom
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }

            return y + height;
        }
    }

    public Point TopLeft => new(Left, Top);

    public Point TopRight => new(Right, Top);

    public Point BottomLeft => new(Left, Bottom);

    public Point BottomRight => new(Right, Bottom);

    public bool Contains(Point point)
    {
        return Contains(point.X, point.Y);
    }

    public bool Contains(int _x, int _y)
    {
        if (IsEmpty)
        {
            return false;
        }

        return ContainsInternal(_x, _y);
    }

    public bool Contains(Rect rect)
    {
        if (IsEmpty || rect.IsEmpty)
        {
            return false;
        }

        return x <= rect.x && y <= rect.y && x + width >= rect.x + rect.width &&
               y + height >= rect.y + rect.height;
    }

    public bool IntersectsWith(Rect rect)
    {
        if (IsEmpty || rect.IsEmpty)
        {
            return false;
        }

        return rect.Left <= Right && rect.Right >= Left && rect.Top <= Bottom &&
               rect.Bottom >= Top;
    }

    public void Intersect(Rect rect)
    {
        if (!IntersectsWith(rect))
        {
            this = Empty;
        }
        else
        {
            int num = Math.Max(Left, rect.Left);
            int num2 = Math.Max(Top, rect.Top);
            width = Math.Max(Math.Min(Right, rect.Right) - num, 0);
            height = Math.Max(Math.Min(Bottom, rect.Bottom) - num2, 0);
            x = num;
            y = num2;
        }
    }

    public static Rect Intersect(Rect rect1, Rect rect2)
    {
        rect1.Intersect(rect2);
        return rect1;
    }

    public void Union(Rect rect)
    {
        if (IsEmpty)
        {
            this = rect;
        }
        else if (!rect.IsEmpty)
        {
            int num = Math.Min(Left, rect.Left);
            int num2 = Math.Min(Top, rect.Top);
            if (rect.Width == int.MaxValue || Width == int.MaxValue)
            {
                width = int.MaxValue;
            }
            else
            {
                int num3 = Math.Max(Right, rect.Right);
                width = Math.Max(num3 - num, 0);
            }

            if (rect.Height == int.MaxValue || Height == int.MaxValue)
            {
                height = int.MaxValue;
            }
            else
            {
                int num4 = Math.Max(Bottom, rect.Bottom);
                height = Math.Max(num4 - num2, 0);
            }

            x = num;
            y = num2;
        }
    }

    public static Rect Union(Rect rect1, Rect rect2)
    {
        rect1.Union(rect2);
        return rect1;
    }

    public void Union(Point point)
    {
        Union(new Rect(point, point));
    }

    public static Rect Union(Rect rect, Point point)
    {
        rect.Union(new Rect(point, point));
        return rect;
    }

    public void Offset(Vector offsetVector)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Rect_CannotCallMethod");
        }

        x += offsetVector.X;
        y += offsetVector.Y;
    }

    public void Offset(int offsetX, int offsetY)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Rect_CannotCallMethod");
        }

        x += offsetX;
        y += offsetY;
    }

    public static Rect Offset(Rect rect, Vector offsetVector)
    {
        rect.Offset(offsetVector.X, offsetVector.Y);
        return rect;
    }

    public static Rect Offset(Rect rect, int offsetX, int offsetY)
    {
        rect.Offset(offsetX, offsetY);
        return rect;
    }

    public void Inflate(Size size)
    {
        Inflate(size.width, size.height);
    }

    public void Inflate(int _width, int _height)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Rect_CannotCallMethod");
        }

        x -= _width;
        y -= _height;
        width += _width;
        width += _width;
        height += _height;
        height += _height;
        if (width < 0 || height < 0)
        {
            this = SEmpty;
        }
    }

    public static Rect Inflate(Rect rect, Size size)
    {
        rect.Inflate(size.width, size.height);
        return rect;
    }

    public static Rect Inflate(Rect rect, int width, int height)
    {
        rect.Inflate(width, height);
        return rect;
    }


    private bool ContainsInternal(int _x, int _y)
    {
        // исправлено нестрогое условие на строгое
        // чтобы в rect(1;1;1;1) попадал только 1 пиксель (1;1) а не 4 пикселя (1;1)-(2;2)
        return _x >= x && _x - width < x && _y >= y && _y - height < y;
        //return ((((_x >= this.x) && ((_x - this.width) <= this.x)) && (_y >= this.y)) && ((_y - this.height) <= this.y));
    }

    private static Rect CreateEmptyRect()
    {
        Rect rect = new Rect
        {
            x = 0,
            y = 0,
            width = 0,
            height = 0
        };
        return rect;
    }

    static Rect()
    {
        SEmpty = CreateEmptyRect();
    }
}