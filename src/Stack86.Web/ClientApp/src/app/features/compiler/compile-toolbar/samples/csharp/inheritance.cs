// Inheritance — single inheritance with base constructor
using System;

class Shape
{
    public int X;
    public int Y;

    public Shape(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int Describe()
    {
        Console.Write("Position: ");
        Console.Write(X);
        Console.Write(", ");
        Console.WriteLine(Y);
        return 0;
    }
}

class Circle : Shape
{
    public int Radius;

    public Circle(int x, int y, int r) : base(x, y)
    {
        Radius = r;
    }

    public int Area()
    {
        return 3 * Radius * Radius;
    }
}

class Program
{
    static int Main()
    {
        Circle c = new Circle(5, 10, 7);
        c.Describe();
        Console.WriteLine("Radius: " + c.Radius);
        Console.WriteLine("Approx area: " + c.Area());

        return 0;
    }
}
