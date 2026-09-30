import helloArithmetic from './samples/js/hello-arithmetic.js?raw';
import stringsAndOutput from './samples/js/strings-and-output.js?raw';
import functionsAndRecursion from './samples/js/functions-and-recursion.js?raw';
import loopsAndControl from './samples/js/loops-and-control.js?raw';
import arraysAndIteration from './samples/js/arrays-and-iteration.js?raw';
import switchAndTernary from './samples/js/switch-and-ternary.js?raw';
import logicalOperators from './samples/js/logical-operators.js?raw';
import bitwiseOperations from './samples/js/bitwise-operations.js?raw';
import objectsAndProperties from './samples/js/objects-and-properties.js?raw';
import arrowFunctions from './samples/js/arrow-functions.js?raw';
import scopeAndConstants from './samples/js/scope-and-constants.js?raw';
import bubbleSort from './samples/js/bubble-sort.js?raw';
import { SampleProgram } from './sample-programs';

function single(source: string): { name: string; content: string }[] {
  return [{ name: 'main.js', content: source }];
}

export const JS_SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Hello Arithmetic', files: single(helloArithmetic) },
  { name: 'Strings & Output', files: single(stringsAndOutput) },
  { name: 'Functions & Recursion', files: single(functionsAndRecursion) },
  { name: 'Loops & Control Flow', files: single(loopsAndControl) },
  { name: 'Arrays & Iteration', files: single(arraysAndIteration) },
  { name: 'Switch & Ternary', files: single(switchAndTernary) },
  { name: 'Logical Operators', files: single(logicalOperators) },
  { name: 'Bitwise Operations', files: single(bitwiseOperations) },
  { name: 'Objects & Properties', files: single(objectsAndProperties) },
  { name: 'Arrow Functions', files: single(arrowFunctions) },
  { name: 'Scope & Constants', files: single(scopeAndConstants) },
  { name: 'Bubble Sort', files: single(bubbleSort) },
];
