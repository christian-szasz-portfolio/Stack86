// Logical & Comparison Operators
let a = 10;
let b = 20;
let c = 10;

// Strict equality
console.log("a === c:", a === c);
console.log("a === b:", a === b);

// Not equal
console.log("a !== b:", a !== b);

// Comparisons
console.log("a < b:", a < b);
console.log("a > b:", a > b);
console.log("a <= c:", a <= c);
console.log("a >= b:", a >= b);

// Logical AND (short-circuit)
let x = 5;
let result = x > 0 && x < 10;
console.log("5 in (0,10):", result);

// Logical OR (short-circuit)
let zero = 0;
let fallback = zero || 42;
console.log("0 || 42:", fallback);

// Logical NOT
console.log("!0:", !zero);
console.log("!1:", !1);

// Chained comparisons via &&
let val = 15;
let inRange = val >= 10 && val <= 20;
console.log("15 in [10,20]:", inRange);

// Ternary with logical
let sign = val > 0 ? 1 : val < 0 ? -1 : 0;
console.log("sign(15):", sign);
