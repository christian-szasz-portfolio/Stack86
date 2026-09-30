// Arrays & Iteration
const numbers = [10, 20, 30, 40, 50];

// Index-based access
console.log(numbers[0]);
console.log(numbers[2]);
console.log(numbers[4]);

// Array length
console.log(numbers.length);

// Modify elements
numbers[1] = 25;
console.log(numbers[1]);

// for-of loop
for (const n of numbers) {
  console.log(n);
}

// Sum via for loop
let total = 0;
for (let i = 0; i < numbers.length; i++) {
  total += numbers[i];
}
console.log(total);

// Compute min and max
let min = numbers[0];
let max = numbers[0];
for (let i = 1; i < numbers.length; i++) {
  if (numbers[i] < min) min = numbers[i];
  if (numbers[i] > max) max = numbers[i];
}
console.log(min);
console.log(max);
