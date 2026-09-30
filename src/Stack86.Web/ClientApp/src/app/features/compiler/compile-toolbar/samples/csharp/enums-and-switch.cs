// Enums & Switch — enum types with switch statement
using System;

enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}

class Program
{
    static int DaysInSeason(Season s)
    {
        int days = 0;
        switch (s)
        {
            case Season.Spring:
                days = 92;
                break;
            case Season.Summer:
                days = 93;
                break;
            case Season.Autumn:
                days = 90;
                break;
            case Season.Winter:
                days = 90;
                break;
        }
        return days;
    }

    static int Main()
    {
        Season current = Season.Summer;
        int days = DaysInSeason(current);

        Console.WriteLine("Season: Summer");
        Console.WriteLine("Days: " + days);

        Season next = Season.Autumn;
        Console.WriteLine("Next season days: " + DaysInSeason(next));

        return 0;
    }
}
