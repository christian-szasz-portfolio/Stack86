// Typed Functions — parameter and return types
function add(a: number, b: number): number {
  return a + b;
}

function factorial(n: number): number {
  if (n <= 1) return 1;
  return n * factorial(n - 1);
}

function isEven(n: number): boolean {
  return n % 2 === 0;
}

console.log(add(3, 7));
console.log(factorial(6));
console.log(isEven(4) ? 1 : 0);
console.log(isEven(7) ? 1 : 0);

// Arrow functions with types
const double = (x: number): number => x * 2;
const square = (x: number): number => x * x;

console.log(double(5));
console.log(square(7));
