// Inheritance — single inheritance with method override
#include <iostream>
using namespace std;

class Shape {
public:
    int x;
    int y;

    Shape(int x, int y) : x(x), y(y) {}

    int describe() {
        cout << "Position: " << x << ", " << y << endl;
        return 0;
    }
};

class Circle : public Shape {
public:
    int radius;

    Circle(int x, int y, int r) : Shape(x, y), radius(r) {}

    int area() {
        return 3 * radius * radius;
    }
};

int main() {
    Circle c(5, 10, 7);
    c.describe();
    cout << "Radius: " << c.radius << endl;
    cout << "Approx area: " << c.area() << endl;

    return 0;
}
