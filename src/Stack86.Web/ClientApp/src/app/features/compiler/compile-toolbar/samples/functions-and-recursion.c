// Functions & Recursion — calls, parameters, stack frames
int factorial(int n) {
    if (n <= 1)
        return 1;
    return n * factorial(n - 1);
}

int fibonacci(int n) {
    if (n <= 0)
        return 0;
    if (n == 1)
        return 1;
    return fibonacci(n - 1) + fibonacci(n - 2);
}

int max(int a, int b) {
    return (a > b) ? a : b;
}

int clamp(int val, int lo, int hi) {
    if (val < lo) return lo;
    if (val > hi) return hi;
    return val;
}

int main() {
    int f5 = factorial(5);
    int fib7 = fibonacci(7);
    int m = max(f5, fib7);
    int c = clamp(200, 0, 100);
    return f5 + fib7 + m + c;
}
