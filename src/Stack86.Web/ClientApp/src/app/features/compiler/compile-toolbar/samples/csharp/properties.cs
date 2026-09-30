// Properties — auto-properties and computed properties
using System;

class Temperature
{
    public int Celsius { get; set; }

    public int Fahrenheit
    {
        get { return Celsius * 9 / 5 + 32; }
    }

    public Temperature(int c)
    {
        Celsius = c;
    }
}

class Program
{
    static int Main()
    {
        Temperature t = new Temperature(100);
        Console.WriteLine("Celsius: " + t.Celsius);
        Console.WriteLine("Fahrenheit: " + t.Fahrenheit);

        t.Celsius = 0;
        Console.WriteLine("Celsius: " + t.Celsius);
        Console.WriteLine("Fahrenheit: " + t.Fahrenheit);

        return 0;
    }
}
