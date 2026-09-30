import { SupportedLanguage } from '../../../core/compiler/compiler.models';
import helloArithmetic from './samples/hello-arithmetic.c?raw';
import structsAndMembers from './samples/structs-and-members.c?raw';
import enumsAndSwitch from './samples/enums-and-switch.c?raw';
import pointersAndArrays from './samples/pointers-and-arrays.c?raw';
import unionsAndTypedefs from './samples/unions-and-typedefs.c?raw';
import loopsAndControl from './samples/loops-and-control.c?raw';
import compoundExpressions from './samples/compound-expressions.c?raw';
import functionsAndRecursion from './samples/functions-and-recursion.c?raw';
import printfShowcase from './samples/printf-showcase.c?raw';
import stdlibShowcase from './samples/stdlib-showcase.c?raw';
import astarMain from './samples/astar/main.c?raw';
import astarHeader from './samples/astar/astar.h?raw';
import astarImpl from './samples/astar/astar.c?raw';
import { CPP_SAMPLE_PROGRAMS } from './cpp-sample-programs';
import { CSHARP_SAMPLE_PROGRAMS } from './csharp-sample-programs';
import { JS_SAMPLE_PROGRAMS } from './js-sample-programs';
import { TS_SAMPLE_PROGRAMS } from './ts-sample-programs';

export interface SampleFile {
  name: string;
  content: string;
}

export interface SampleProgram {
  name: string;
  files: SampleFile[];
}

function single(source: string): SampleFile[] {
  return [{ name: 'main.c', content: source }];
}

export const C_SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Hello Arithmetic', files: single(helloArithmetic) },
  { name: 'Structs & Members', files: single(structsAndMembers) },
  { name: 'Enums & Switch', files: single(enumsAndSwitch) },
  { name: 'Pointers & Arrays', files: single(pointersAndArrays) },
  { name: 'Unions & Typedefs', files: single(unionsAndTypedefs) },
  { name: 'Loops & Control Flow', files: single(loopsAndControl) },
  { name: 'Compound Expressions', files: single(compoundExpressions) },
  { name: 'Functions & Recursion', files: single(functionsAndRecursion) },
  { name: 'Printf Showcase', files: single(printfShowcase) },
  { name: 'Standard Library', files: single(stdlibShowcase) },
  {
    name: 'A* Pathfinding',
    files: [
      { name: 'main.c', content: astarMain },
      { name: 'astar.h', content: astarHeader },
      { name: 'astar.c', content: astarImpl },
    ],
  },
];

/** @deprecated Use {@link SAMPLE_REGISTRY} with a specific language instead. */
export const SAMPLE_PROGRAMS = C_SAMPLE_PROGRAMS;

export const DEFAULT_SAMPLE_INDEX = 0;

export const SAMPLE_REGISTRY: Partial<Record<SupportedLanguage, SampleProgram[]>> = {
  [SupportedLanguage.C]: C_SAMPLE_PROGRAMS,
  [SupportedLanguage.Cpp]: CPP_SAMPLE_PROGRAMS,
  [SupportedLanguage.CSharp]: CSHARP_SAMPLE_PROGRAMS,
  [SupportedLanguage.JavaScript]: JS_SAMPLE_PROGRAMS,
  [SupportedLanguage.TypeScript]: TS_SAMPLE_PROGRAMS,
};

export function getSamplesForLanguage(language: SupportedLanguage): SampleProgram[] {
  return SAMPLE_REGISTRY[language] ?? [];
}

export function getDefaultSample(language: SupportedLanguage): SampleProgram | undefined {
  const samples = getSamplesForLanguage(language);
  return samples.length > 0 ? samples[0] : undefined;
}
