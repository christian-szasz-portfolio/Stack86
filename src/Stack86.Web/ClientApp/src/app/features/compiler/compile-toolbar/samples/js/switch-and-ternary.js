// Switch & Ternary Expressions
function dayName(day) {
  switch (day) {
    case 1: return 1;
    case 2: return 2;
    case 3: return 3;
    case 4: return 4;
    case 5: return 5;
    case 6: return 6;
    case 7: return 7;
    default: return 0;
  }
}

console.log(dayName(1));
console.log(dayName(5));
console.log(dayName(7));
console.log(dayName(99));

// Ternary operator
let score = 85;
let passed = score >= 60 ? 1 : 0;
console.log(passed);

// Nested ternary
let grade = score >= 90 ? 5 : score >= 80 ? 4 : score >= 70 ? 3 : score >= 60 ? 2 : 1;
console.log(grade);

// Switch with fall-through (no break)
let val = 2;
let result = 0;
switch (val) {
  case 1:
    result += 10;
    break;
  case 2:
    result += 20;
    break;
  case 3:
    result += 30;
    break;
}
console.log(result);
