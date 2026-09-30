// Enums & Constants — TypeScript enums with typed variables

enum Direction {
  Up,
  Down,
  Left,
  Right
}

const dir: Direction = Direction.Right;
console.log(dir);

if (dir === Direction.Right) {
  console.log(1);
} else {
  console.log(0);
}

// Enum with explicit values
enum Color {
  Red = 10,
  Green = 20,
  Blue = 30
}

const c: Color = Color.Green;
console.log(c);

// Const declarations with types
const MAX_SIZE: number = 100;
const PI_APPROX: number = 3;
console.log(MAX_SIZE);
console.log(PI_APPROX);

// Let vs const
let counter: number = 0;
counter += 10;
counter += 20;
console.log(counter);
