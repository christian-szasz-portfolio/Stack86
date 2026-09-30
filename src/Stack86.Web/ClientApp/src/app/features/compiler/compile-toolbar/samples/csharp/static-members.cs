// Static Members — static fields and methods
using System;

class Counter
{
    public static int Count;

    public static int Increment()
    {
        Count = Count + 1;
        return Count;
    }

    public static int Reset()
    {
        Count = 0;
        return 0;
    }
}

class Program
{
    static int Main()
    {
        Counter.Reset();

        Console.WriteLine("Count: " + Counter.Count);
        Counter.Increment();
        Counter.Increment();
        Counter.Increment();
        Console.WriteLine("After 3 increments: " + Counter.Count);

        Counter.Reset();
        Console.WriteLine("After reset: " + Counter.Count);

        return 0;
    }
}
