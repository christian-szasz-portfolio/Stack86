// References & Pointers — pass by reference vs pointer
#include <iostream>
using namespace std;

void swapByRef(int& a, int& b) {
    int temp = a;
    a = b;
    b = temp;
}

void swapByPtr(int* a, int* b) {
    int temp = *a;
    *a = *b;
    *b = temp;
}

int main() {
    int x = 10;
    int y = 20;

    cout << "Before swap: x=" << x << " y=" << y << endl;
    swapByRef(x, y);
    cout << "After ref swap: x=" << x << " y=" << y << endl;
    swapByPtr(&x, &y);
    cout << "After ptr swap: x=" << x << " y=" << y << endl;

    return 0;
}
