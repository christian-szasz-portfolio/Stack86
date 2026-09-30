// Sorting Algorithm — Bubble Sort on 8086
const arr = [64, 34, 25, 12, 22, 11, 90, 1, 45, 78];

// Print original array
console.log("Before:");
for (let i = 0; i < arr.length; i++) {
  console.log(arr[i]);
}

// Bubble sort
for (let i = 0; i < arr.length - 1; i++) {
  for (let j = 0; j < arr.length - i - 1; j++) {
    if (arr[j] > arr[j + 1]) {
      // Swap
      let temp = arr[j];
      arr[j] = arr[j + 1];
      arr[j + 1] = temp;
    }
  }
}

// Print sorted array
console.log("After:");
for (let i = 0; i < arr.length; i++) {
  console.log(arr[i]);
}

// Verify sorted: check each pair
let sorted = 1;
for (let i = 0; i < arr.length - 1; i++) {
  if (arr[i] > arr[i + 1]) {
    sorted = 0;
  }
}
console.log("Sorted:", sorted);
