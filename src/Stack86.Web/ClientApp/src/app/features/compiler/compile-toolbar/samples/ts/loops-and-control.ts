// Loops & Control Flow — typed loop variables
function fizzbuzz(n: number): void {
  for (let i: number = 1; i <= n; i++) {
    if (i % 3 === 0) {
      console.log(0);
    } else if (i % 5 === 0) {
      console.log(1);
    } else {
      console.log(i);
    }
  }
}

fizzbuzz(15);

// While loop with typed accumulator
function sumTo(n: number): number {
  let total: number = 0;
  let i: number = 1;
  while (i <= n) {
    total += i;
    i++;
  }
  return total;
}

console.log(sumTo(10));

// Do-while
let x: number = 1;
do {
  x *= 2;
} while (x < 100);
console.log(x);
