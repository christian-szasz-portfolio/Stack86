// Pointers & Arrays — indexing, address-of, dereference
int sum_array(int* arr, int len) {
    int total = 0;
    int i = 0;
    while (i < len) {
        total += arr[i];
        i++;
    }
    return total;
}

void swap(int* a, int* b) {
    int temp = *a;
    *a = *b;
    *b = temp;
}

int main() {
    int nums[5];
    nums[0] = 10;
    nums[1] = 20;
    nums[2] = 30;
    nums[3] = 40;
    nums[4] = 50;

    int total = sum_array(nums, 5);

    swap(&nums[0], &nums[4]);

    int first = nums[0];
    int last = nums[4];

    return total + first + last;
}
