// Default Parameters — functions with default argument values
#include <iostream>
using namespace std;

int power(int base, int exp = 2) {
    int result = 1;
    for (int i = 0; i < exp; i++) {
        result = result * base;
    }
    return result;
}

void greet(int times = 1) {
    for (int i = 0; i < times; i++) {
        cout << "Hello!" << endl;
    }
}

int main() {
    cout << "3^2 = " << power(3) << endl;
    cout << "2^8 = " << power(2, 8) << endl;
    cout << "5^3 = " << power(5, 3) << endl;

    greet();
    greet(3);

    return 0;
}
