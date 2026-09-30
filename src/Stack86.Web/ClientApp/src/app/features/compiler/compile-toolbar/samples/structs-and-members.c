// Structs — member access with dot and arrow operators
struct Point {
    int x;
    int y;
};

int distance_squared(struct Point* p1, struct Point* p2) {
    int dx = p2->x - p1->x;
    int dy = p2->y - p1->y;
    return dx * dx + dy * dy;
}

int main() {
    struct Point a;
    a.x = 3;
    a.y = 4;

    struct Point b;
    b.x = 7;
    b.y = 1;

    int dist2 = distance_squared(&a, &b);
    return dist2;
}
