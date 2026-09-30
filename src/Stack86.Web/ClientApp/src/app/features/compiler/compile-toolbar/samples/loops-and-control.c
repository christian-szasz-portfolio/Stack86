// Loops & Control Flow — for, while, do-while, break, continue
int main() {
    // For loop: compute sum 1..10
    int sum = 0;
    int i;
    for (i = 1; i <= 10; i++) {
        sum += i;
    }

    // While loop with continue: sum odd numbers 1..20
    int odd_sum = 0;
    int j = 0;
    while (j < 20) {
        j++;
        if (j % 2 == 0)
            continue;
        odd_sum += j;
    }

    // Do-while loop with break: find first multiple of 7 > 50
    int n = 50;
    do {
        n++;
    } while (n % 7 != 0);

    return sum + odd_sum + n;
}
