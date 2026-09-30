// Bubble Sort — classic algorithm with TypeScript types
function bubbleSort(arr: number[]): void {
  const n: number = 5;
  for (let i: number = 0; i < n - 1; i++) {
    for (let j: number = 0; j < n - i - 1; j++) {
      if (arr[j] > arr[j + 1]) {
        const temp: number = arr[j];
        arr[j] = arr[j + 1];
        arr[j + 1] = temp;
      }
    }
  }
}

const data: number[] = [64, 34, 25, 12, 22];
bubbleSort(data);

for (const val of data) {
  console.log(val);
}

// Typed helper: find minimum
function findMin(arr: number[]): number {
  let min: number = arr[0];
  for (let i: number = 1; i < 5; i++) {
    if (arr[i] < min) {
      min = arr[i];
    }
  }
  return min;
}

const values: number[] = [38, 12, 45, 7, 23];
console.log(findMin(values));
