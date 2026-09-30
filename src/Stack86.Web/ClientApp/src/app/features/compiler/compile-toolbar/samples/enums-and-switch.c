// Enums & Switch — symbolic constants with control flow
enum Color {
    Red,
    Green,
    Blue,
    Yellow
};

int color_value(enum Color c) {
    int result;
    switch (c) {
        case Red:
            result = 0xFF00;
            break;
        case Green:
            result = 0x00FF;
            break;
        case Blue:
            result = 0x0F0F;
            break;
        default:
            result = 0;
            break;
    }
    return result;
}

int main() {
    int r = color_value(Red);
    int g = color_value(Green);
    int b = color_value(Blue);
    int y = color_value(Yellow);
    return r + g + b + y;
}
