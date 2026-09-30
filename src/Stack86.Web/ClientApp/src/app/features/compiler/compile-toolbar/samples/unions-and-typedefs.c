// Unions & Typedefs — type aliases and overlapping storage
typedef int i16;
typedef char byte;

union Value {
    i16 word;
    byte lo;
};

struct Register {
    union Value val;
    byte flags;
};

int main() {
    struct Register reg;
    reg.val.word = 0x1234;
    reg.flags = 1;

    byte low_byte = reg.val.lo;

    i16 combined = reg.val.word + low_byte;
    return combined;
}
