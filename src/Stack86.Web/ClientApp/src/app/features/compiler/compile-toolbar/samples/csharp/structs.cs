// Structs — value types with methods
using System;

struct Point
{
    public int X;
    public int Y;

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int DistanceSquared(Point other)
    {
        int dx = X - other.X;
        int dy = Y - other.Y;
        return dx * dx + dy * dy;
    }
}

class Program
{
    static int Main()
    {
        Point a = new Point(3, 4);
        Point b = new Point(7, 1);

        Console.Write("Point A: ");
        Console.Write(a.X);
        Console.Write(", ");
        Console.WriteLine(a.Y);
        Console.Write("Point B: ");
        Console.Write(b.X);
        Console.Write(", ");
        Console.WriteLine(b.Y);
        Console.Write("Distance squared: ");
        Console.WriteLine(a.DistanceSquared(b));

        return 0;
    }
}
