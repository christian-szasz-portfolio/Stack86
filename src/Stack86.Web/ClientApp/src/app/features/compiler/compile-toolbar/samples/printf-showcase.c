// Printf Showcase — strings, integers, chars, expressions
#include <stdio.h>

int abs(int x) {
    if (x < 0)
        return -x;
    return x;
}

int main() {
    printf("=== Printf Showcase ===\n");

    // Basic integers
    printf("One: %d\n", 1);
    printf("Negative: %d\n", -42);
    printf("Zero: %d\n", 0);

    // Arithmetic expressions
    int a = 17;
    int b = 5;
    printf("%d + %d = %d\n", a, b, a + b);
    printf("%d - %d = %d\n", a, b, a - b);
    printf("%d * %d = %d\n", a, b, a * b);
    printf("%d / %d = %d\n", a, b, a / b);
    printf("%d %% %d = %d\n", a, b, a % b);

    // Character output
    printf("Chars: %c%c%c%c%c\n", 72, 101, 108, 108, 111);

    // Loop with printf
    printf("Counting: ");
    int i;
    for (i = 1; i <= 5; i++) {
        printf("%d ", i);
    }
    printf("\n");

    // Fibonacci sequence
    printf("Fibonacci: ");
    int prev = 0;
    int curr = 1;
    for (i = 0; i < 10; i++) {
        printf("%d ", prev);
        int next = prev + curr;
        prev = curr;
        curr = next;
    }
    printf("\n");

    // Conditional messages
    int score = 85;
    printf("Score: %d - ", score);
    if (score >= 90)
        printf("Excellent!\n");
    else if (score >= 80)
        printf("Good job!\n");
    else if (score >= 70)
        printf("Not bad.\n");
    else
        printf("Keep trying.\n");

    // Absolute value via function call
    printf("abs(-99) = %d\n", abs(-99));
    printf("abs(42) = %d\n", abs(42));

    // Multiplication table row
    printf("5x table: ");
    for (i = 1; i <= 9; i++) {
        printf("%d ", 5 * i);
    }
    printf("\n");

    // Signed edge cases
    printf("Max signed: %d\n", 32767);
    printf("Min signed: %d\n", -32768);

    printf("=== Done ===\n");
    return 0;
}
