// Objects & Properties
const point = { x: 10, y: 20 };
console.log(point.x);
console.log(point.y);

// Modify properties
point.x = 30;
console.log(point.x);

// Object as function parameter
function distance(p) {
  // Manhattan distance from origin
  let dx = p.x;
  let dy = p.y;
  if (dx < 0) dx = -dx;
  if (dy < 0) dy = -dy;
  return dx + dy;
}

console.log(distance(point));

// Another object
const rect = { width: 15, height: 8 };
function area(r) {
  return r.width * r.height;
}
function perimeter(r) {
  return 2 * (r.width + r.height);
}
console.log(area(rect));
console.log(perimeter(rect));
