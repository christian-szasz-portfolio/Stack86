// Loops & Control — for, while, do-while, switch
#include <iostream>
using namespace std;

int main() {
    // Fizzbuzz with for loop
    cout << "FizzBuzz:" << endl;
    for (int i = 1; i <= 20; i++) {
        if (i % 15 == 0) {
            cout << "FizzBuzz" << endl;
        } else if (i % 3 == 0) {
            cout << "Fizz" << endl;
        } else if (i % 5 == 0) {
            cout << "Buzz" << endl;
        } else {
            cout << i << endl;
        }
    }

    // Countdown with while
    int n = 5;
    cout << "Countdown: ";
    while (n > 0) {
        cout << n << " ";
        n--;
    }
    cout << endl;

    // Day of week with switch
    int day = 3;
    switch (day) {
        case 1: cout << "Monday" << endl; break;
        case 2: cout << "Tuesday" << endl; break;
        case 3: cout << "Wednesday" << endl; break;
        case 4: cout << "Thursday" << endl; break;
        case 5: cout << "Friday" << endl; break;
        default: cout << "Weekend" << endl; break;
    }

    return 0;
}
