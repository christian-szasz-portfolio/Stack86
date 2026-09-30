// Loops & Control Flow
// For loop — sum 1 to 10
let sum = 0;
for (let i = 1; i <= 10; i++) {
  sum += i;
}
console.log(sum);

// While loop — countdown
let count = 5;
while (count > 0) {
  console.log(count);
  count--;
}

// Do-while loop
let x = 1;
do {
  console.log(x);
  x *= 2;
} while (x <= 16);

// Break and continue
for (let i = 0; i < 10; i++) {
  if (i === 3) continue;
  if (i === 7) break;
  console.log(i);
}

// Nested loops
for (let i = 1; i <= 3; i++) {
  for (let j = 1; j <= 3; j++) {
    console.log(i * 10 + j);
  }
}
