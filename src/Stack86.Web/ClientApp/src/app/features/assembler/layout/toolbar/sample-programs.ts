import simpleArithmetic from './samples/simple-arithmetic.asm?raw';
import counterLoop from './samples/counter-loop.asm?raw';
import fibonacci from './samples/fibonacci.asm?raw';
import helloWorld from './samples/hello-world.asm?raw';
import stackOperations from './samples/stack-operations.asm?raw';
import factorial from './samples/factorial.asm?raw';
import subroutineCall from './samples/subroutine-call.asm?raw';
import bitwiseOps from './samples/bitwise-ops.asm?raw';
import conditionalBranch from './samples/conditional-branch.asm?raw';
import memoryAccess from './samples/memory-access.asm?raw';

export interface SampleProgram {
  name: string;
  source: string;
}

export const SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Simple Arithmetic', source: simpleArithmetic },
  { name: 'Counter Loop', source: counterLoop },
  { name: 'Fibonacci', source: fibonacci },
  { name: 'Factorial', source: factorial },
  { name: 'Hello World (INT 21h)', source: helloWorld },
  { name: 'Stack Operations', source: stackOperations },
  { name: 'Subroutine Call', source: subroutineCall },
  { name: 'Bitwise Operations', source: bitwiseOps },
  { name: 'Conditional Branch', source: conditionalBranch },
  { name: 'Memory Access', source: memoryAccess },
];
