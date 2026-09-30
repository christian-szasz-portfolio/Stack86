// Bitwise Operations
let a = 170;   // 0xAA = 10101010
let b = 204;   // 0xCC = 11001100

console.log(a & b);    // AND
console.log(a | b);    // OR
console.log(a ^ b);    // XOR
console.log(~a);       // NOT (bitwise complement)

// Shift operations
let x = 1;
console.log(x << 1);   // 2
console.log(x << 4);   // 16
console.log(x << 8);   // 256

let y = 256;
console.log(y >> 1);   // 128
console.log(y >> 4);   // 16
console.log(y >> 8);   // 1

// Compound bitwise assignment
let flags = 0;
flags |= 1;     // set bit 0
flags |= 4;     // set bit 2
console.log(flags);

flags &= ~1;    // clear bit 0
console.log(flags);

flags ^= 8;     // toggle bit 3
console.log(flags);
