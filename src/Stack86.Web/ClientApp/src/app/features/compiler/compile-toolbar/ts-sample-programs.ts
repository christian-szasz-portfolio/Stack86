import helloArithmetic from './samples/ts/hello-arithmetic.ts?raw';
import typedFunctions from './samples/ts/typed-functions.ts?raw';
import enumsAndConstants from './samples/ts/enums-and-constants.ts?raw';
import loopsAndControl from './samples/ts/loops-and-control.ts?raw';
import arraysAndIteration from './samples/ts/arrays-and-iteration.ts?raw';
import switchAndTernary from './samples/ts/switch-and-ternary.ts?raw';
import objectsAndInterfaces from './samples/ts/objects-and-interfaces.ts?raw';
import bitwiseAndLogical from './samples/ts/bitwise-and-logical.ts?raw';
import bubbleSort from './samples/ts/bubble-sort.ts?raw';
import { SampleProgram } from './sample-programs';

function single(source: string): { name: string; content: string }[] {
  return [{ name: 'main.ts', content: source }];
}

export const TS_SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Hello Arithmetic', files: single(helloArithmetic) },
  { name: 'Typed Functions', files: single(typedFunctions) },
  { name: 'Enums & Constants', files: single(enumsAndConstants) },
  { name: 'Loops & Control Flow', files: single(loopsAndControl) },
  { name: 'Arrays & Iteration', files: single(arraysAndIteration) },
  { name: 'Switch & Ternary', files: single(switchAndTernary) },
  { name: 'Objects & Interfaces', files: single(objectsAndInterfaces) },
  { name: 'Bitwise & Logical', files: single(bitwiseAndLogical) },
  { name: 'Bubble Sort', files: single(bubbleSort) },
];
