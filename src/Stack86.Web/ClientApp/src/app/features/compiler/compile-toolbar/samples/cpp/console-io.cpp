// Console I/O — cout and cin usage
#include <iostream>
using namespace std;

int main() {
    int age;
    char grade;

    cout << "Enter your age: ";
    cin >> age;

    cout << "Enter your grade (A-F): ";
    cin >> grade;

    cout << "Age: " << age << endl;
    cout << "Grade: " << grade << endl;

    if (age >= 18) {
        cout << "Status: Adult" << endl;
    } else {
        cout << "Status: Minor" << endl;
    }

    return 0;
}
