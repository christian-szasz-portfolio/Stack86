// Compound Expressions — +=, ++, ternary, sizeof, bitwise
int main() {
    int a = 10;
    int b = 3;

    // Compound assignments
    a += 5;
    a -= 2;
    a *= b;
    a /= 2;
    a %= 7;

    // Increment and decrement
    int pre = ++a;
    int post = b++;
    int pre_d = --a;
    int post_d = b--;

    // Ternary operator
    int max = (a > b) ? a : b;
    int min = (a < b) ? a : b;

    // Sizeof
    int int_size = sizeof(int);
    int char_size = sizeof(char);

    // Bitwise operations
    int bits = 0xAB;
    bits &= 0x0F;
    bits |= 0x30;
    bits ^= 0xFF;
    int shifted = bits << 2;

    return max + min + int_size + char_size + shifted;
}
