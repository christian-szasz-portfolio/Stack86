// Loops & Control Flow — for, while, do-while, if/else
using System;

class Program
{
    static int Main()
    {
        // For loop — sum 1 to 10
        int sum = 0;
        for (int i = 1; i <= 10; i++)
        {
            sum = sum + i;
        }
        Console.WriteLine("Sum 1..10: " + sum);

        // While loop — count down
        int n = 5;
        Console.Write("Countdown: ");
        while (n > 0)
        {
            Console.Write(n);
            Console.Write(" ");
            n--;
        }
        Console.WriteLine("");

        // Do-while — find first power of 2 > 100
        int val = 1;
        do
        {
            val = val * 2;
        } while (val <= 100);
        Console.WriteLine("First power of 2 > 100: " + val);

        // Nested if/else
        int score = 85;
        if (score >= 90)
        {
            Console.WriteLine("Grade: A");
        }
        else if (score >= 80)
        {
            Console.WriteLine("Grade: B");
        }
        else if (score >= 70)
        {
            Console.WriteLine("Grade: C");
        }
        else
        {
            Console.WriteLine("Grade: F");
        }

        return 0;
    }
}
