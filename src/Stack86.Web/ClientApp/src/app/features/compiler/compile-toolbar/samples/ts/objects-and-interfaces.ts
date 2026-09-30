// Objects & Interfaces — typed objects with interface definitions
interface Point {
  x: number;
  y: number;
}

const p: Point = { x: 10, y: 20 };
console.log(p.x);
console.log(p.y);

// Function taking an interface
function manhattan(a: Point, b: Point): number {
  let dx: number = a.x - b.x;
  let dy: number = a.y - b.y;
  if (dx < 0) dx = -dx;
  if (dy < 0) dy = -dy;
  return dx + dy;
}

const p1: Point = { x: 3, y: 7 };
const p2: Point = { x: 10, y: 2 };
console.log(manhattan(p1, p2));

// Nested object access
interface Rect {
  origin: Point;
  width: number;
  height: number;
}

const r: Rect = { origin: { x: 5, y: 5 }, width: 20, height: 10 };
console.log(r.width);
console.log(r.height);
console.log(r.origin.x);
