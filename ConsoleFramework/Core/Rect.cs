using System;

namespace ConsoleFramework.Core;

public struct Rect : IFormattable
{
    private int _x;
    private int _y;
    private int _width;
    private int _height;
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

    private string ConvertToString(string? format, IFormatProvider? provider)
    {
        if (IsEmpty)
        {
            return "Empty";
        }

        const char numericListSeparator = ',';
        return string.Format(provider,
            "{1}{0}{2}{0}{3}{0}{4}",
            numericListSeparator,
            _x.ToString(format),
            _y.ToString(format),
            _width.ToString(format),
            _height.ToString(format));
    }

    public Rect(Rect copy)
    {
        _x = copy._x;
        _y = copy._y;
        _width = copy._width;
        _height = copy._height;
    }

    public Rect(Point location, Size size)
    {
        if (size.IsEmpty)
        {
            this = SEmpty;
        }
        else
        {
            _x = location.X;
            _y = location.Y;
            _width = size.Width;
            _height = size.Height;
        }
    }

    public Rect(int x, int y, int width, int height)
    {
        if (width < 0 || height < 0)
        {
            throw new ArgumentException("Size_WidthAndHeightCannotBeNegative");
        }

        _x = x;
        _y = y;
        _width = width;
        _height = height;
    }

    public Rect(Point point1, Point point2)
    {
        _x = Math.Min(point1.X, point2.X);
        _y = Math.Min(point1.Y, point2.Y);
        _width = Math.Max(Math.Max(point1.X, point2.X) - _x, 0);
        _height = Math.Max(Math.Max(point1.Y, point2.Y) - _y, 0);
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
            _x = _y = 0;
            _width = size.Width;
            _height = size.Height;
        }
    }

    public static Rect Empty => SEmpty;

    public bool IsEmpty => _width == 0 || _height == 0;

    public Point Location
    {
        get => new(_x, _y);
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            _x = value.X;
            _y = value.Y;
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

            return new Size(_width, _height);
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

                _width = value.Width;
                _height = value.Height;
            }
        }
    }

    public int X
    {
        get => _x;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            _x = value;
        }
    }

    public int Y
    {
        get => _y;
        set
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("Rect_CannotModifyEmptyRect");
            }

            _y = value;
        }
    }

    public int Width
    {
        get => _width;
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

            _width = value;
        }
    }

    public int Height
    {
        get => _height;
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

            _height = value;
        }
    }

    public int Left => _x;

    public int Top => _y;

    public int Right
    {
        get
        {
            if (IsEmpty)
            {
                return 0;
            }

            return _x + _width;
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

            return _y + _height;
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

    public bool Contains(int x, int y)
    {
        if (IsEmpty)
        {
            return false;
        }

        return ContainsInternal(x, y);
    }

    public bool Contains(Rect rect)
    {
        if (IsEmpty || rect.IsEmpty)
        {
            return false;
        }

        return _x <= rect._x && _y <= rect._y && _x + _width >= rect._x + rect._width &&
               _y + _height >= rect._y + rect._height;
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
            _width = Math.Max(Math.Min(Right, rect.Right) - num, 0);
            _height = Math.Max(Math.Min(Bottom, rect.Bottom) - num2, 0);
            _x = num;
            _y = num2;
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
                _width = int.MaxValue;
            }
            else
            {
                int num3 = Math.Max(Right, rect.Right);
                _width = Math.Max(num3 - num, 0);
            }

            if (rect.Height == int.MaxValue || Height == int.MaxValue)
            {
                _height = int.MaxValue;
            }
            else
            {
                int num4 = Math.Max(Bottom, rect.Bottom);
                _height = Math.Max(num4 - num2, 0);
            }

            _x = num;
            _y = num2;
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

        _x += offsetVector.X;
        _y += offsetVector.Y;
    }

    public void Offset(int offsetX, int offsetY)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Rect_CannotCallMethod");
        }

        _x += offsetX;
        _y += offsetY;
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
        Inflate(size.Width, size.Height);
    }

    public void Inflate(int width, int height)
    {
        if (IsEmpty)
        {
            throw new InvalidOperationException("Rect_CannotCallMethod");
        }

        _x -= width;
        _y -= height;
        _width += width;
        _width += width;
        _height += height;
        _height += height;
        if (_width < 0 || _height < 0)
        {
            this = SEmpty;
        }
    }

    public static Rect Inflate(Rect rect, Size size)
    {
        rect.Inflate(size.Width, size.Height);
        return rect;
    }

    public static Rect Inflate(Rect rect, int width, int height)
    {
        rect.Inflate(width, height);
        return rect;
    }


    private bool ContainsInternal(int x, int y)
    {
        // исправлено нестрогое условие на строгое
        // чтобы в rect(1;1;1;1) попадал только 1 пиксель (1;1) а не 4 пикселя (1;1)-(2;2)
        return x >= _x && x - _width < _x && y >= _y && y - _height < _y;
        //return ((((x >= this._x) && ((x - this._width) <= this._x)) && (y >= this._y)) && ((y - this._height) <= this._y));
    }

    private static Rect CreateEmptyRect()
    {
        Rect rect = new Rect
        {
            _x = 0,
            _y = 0,
            _width = 0,
            _height = 0
        };
        return rect;
    }

    static Rect()
    {
        SEmpty = CreateEmptyRect();
    }
}