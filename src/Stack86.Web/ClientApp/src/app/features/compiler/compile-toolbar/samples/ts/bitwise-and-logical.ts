// Bitwise & Logical — typed bitwise and logical operations
const a: number = 0xFF;
const b: number = 0x0F;

console.log(a & b);
console.log(a | b);
console.log(a ^ b);
console.log(~b);
console.log(b << 2);
console.log(a >> 4);

// Logical operators with typed variables
const x: number = 5;
const y: number = 0;
const z: number = 10;

console.log(x && z);
console.log(y || z);
console.log(!y ? 1 : 0);

// Combining bitwise in a function
function setBit(value: number, bit: number): number {
  return value | (1 << bit);
}

function clearBit(value: number, bit: number): number {
  return value & ~(1 << bit);
}

let flags: number = 0;
flags = setBit(flags, 0);
flags = setBit(flags, 3);
console.log(flags);
flags = clearBit(flags, 0);
console.log(flags);
