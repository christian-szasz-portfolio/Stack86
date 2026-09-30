// Scope & Constants — var, let, const
const PI_APPROX = 355; // 355/113 ≈ π (integer math)
const DENOM = 113;

// const prevents reassignment (compiler error if uncommented):
// PI_APPROX = 0;

// let has block scope
let total = 0;
for (let i = 1; i <= 5; i++) {
  let squared = i * i;
  total += squared;
}
console.log("Sum of squares 1..5:", total);

// var has function scope (hoisted)
function demo() {
  var x = 10;
  if (x > 5) {
    var y = 20; // y is function-scoped, visible below
  }
  console.log("x:", x);
  console.log("y:", y);
}
demo();

// Nested scope with let
let outer = 1;
{
  let inner = 2;
  console.log("outer:", outer);
  console.log("inner:", inner);
}
// inner is not accessible here

// Multiple const declarations
const WIDTH = 80;
const HEIGHT = 25;
const AREA = WIDTH * HEIGHT;
console.log("Screen area:", AREA);
