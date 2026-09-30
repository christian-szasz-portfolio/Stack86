// Arrow Functions & Expressions
const double = (x) => x * 2;
const square = (x) => x * x;
const add = (a, b) => a + b;

console.log(double(5));
console.log(square(7));
console.log(add(3, 4));

// Arrow with block body
const clamp = (val, min, max) => {
  if (val < min) return min;
  if (val > max) return max;
  return val;
};

console.log(clamp(5, 0, 10));
console.log(clamp(-3, 0, 10));
console.log(clamp(15, 0, 10));

// Passing arrow functions as callbacks
function applyTwice(fn, x) {
  return fn(fn(x));
}

console.log(applyTwice(double, 3));
console.log(applyTwice(square, 2));

// Immediately-useful computations
const isEven = (n) => n % 2 === 0 ? 1 : 0;
console.log(isEven(4));
console.log(isEven(7));
