// Functions & Recursion
function factorial(n) {
  if (n <= 1) {
    return 1;
  }
  return n * factorial(n - 1);
}

function fibonacci(n) {
  if (n <= 0) return 0;
  if (n === 1) return 1;
  return fibonacci(n - 1) + fibonacci(n - 2);
}

function max(a, b) {
  if (a > b) return a;
  return b;
}

console.log(factorial(5));
console.log(factorial(7));

console.log(fibonacci(0));
console.log(fibonacci(1));
console.log(fibonacci(10));

console.log(max(42, 17));
console.log(max(3, 99));
