// Arrays & Iteration — typed arrays and for-of loops
const numbers: number[] = [10, 20, 30, 40, 50];

let sum: number = 0;
for (const n of numbers) {
  sum += n;
}
console.log(sum);

// Array manipulation
const squares: number[] = [1, 4, 9, 16, 25];
for (let i: number = 0; i < 5; i++) {
  console.log(squares[i]);
}

// Typed accumulation
function arraySum(arr: number[]): number {
  let total: number = 0;
  for (const val of arr) {
    total += val;
  }
  return total;
}

console.log(arraySum([5, 10, 15]));
