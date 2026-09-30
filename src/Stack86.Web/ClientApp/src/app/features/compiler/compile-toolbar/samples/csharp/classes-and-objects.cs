// Classes & Objects — fields, methods, constructors
using System;

class Rectangle
{
    public int Width;
    public int Height;

    public Rectangle(int w, int h)
    {
        Width = w;
        Height = h;
    }

    public int Area()
    {
        return Width * Height;
    }

    public int Perimeter()
    {
        return 2 * (Width + Height);
    }
}

class Program
{
    static int Main()
    {
        Rectangle r = new Rectangle(10, 5);

        Console.WriteLine("Width: " + r.Width);
        Console.WriteLine("Height: " + r.Height);
        Console.WriteLine("Area: " + r.Area());
        Console.WriteLine("Perimeter: " + r.Perimeter());

        return 0;
    }
}
