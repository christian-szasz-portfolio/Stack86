// Classes & Objects — struct-like classes with methods
#include <iostream>
using namespace std;

class Rectangle {
public:
    int width;
    int height;

    Rectangle(int w, int h) : width(w), height(h) {}

    int area() {
        return width * height;
    }

    int perimeter() {
        return 2 * (width + height);
    }
};

int main() {
    Rectangle r(10, 5);

    cout << "Width: " << r.width << endl;
    cout << "Height: " << r.height << endl;
    cout << "Area: " << r.area() << endl;
    cout << "Perimeter: " << r.perimeter() << endl;

    return 0;
}
