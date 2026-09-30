// Switch & Ternary — pattern matching and conditional expressions
function dayName(day: number): number {
  switch (day) {
    case 1: return 10;
    case 2: return 20;
    case 3: return 30;
    case 4: return 40;
    case 5: return 50;
    default: return 0;
  }
}

console.log(dayName(3));
console.log(dayName(5));
console.log(dayName(9));

// Ternary with types
const x: number = 42;
const result: number = x > 20 ? 1 : 0;
console.log(result);

// Nested ternary
function classify(n: number): number {
  return n > 0 ? 1 : n < 0 ? -1 : 0;
}

console.log(classify(10));
console.log(classify(-5));
console.log(classify(0));
