import helloArithmetic from './samples/cpp/hello-arithmetic.cpp?raw';
import classesAndObjects from './samples/cpp/classes-and-objects.cpp?raw';
import inheritance from './samples/cpp/inheritance.cpp?raw';
import referencesAndPointers from './samples/cpp/references-and-pointers.cpp?raw';
import consoleIo from './samples/cpp/console-io.cpp?raw';
import newAndDelete from './samples/cpp/new-and-delete.cpp?raw';
import defaultParameters from './samples/cpp/default-parameters.cpp?raw';
import loopsAndControl from './samples/cpp/loops-and-control.cpp?raw';
import { SampleProgram } from './sample-programs';

function single(source: string): { name: string; content: string }[] {
  return [{ name: 'main.cpp', content: source }];
}

export const CPP_SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Hello Arithmetic', files: single(helloArithmetic) },
  { name: 'Classes & Objects', files: single(classesAndObjects) },
  { name: 'Inheritance', files: single(inheritance) },
  { name: 'References & Pointers', files: single(referencesAndPointers) },
  { name: 'Console I/O', files: single(consoleIo) },
  { name: 'New & Delete', files: single(newAndDelete) },
  { name: 'Default Parameters', files: single(defaultParameters) },
  { name: 'Loops & Control Flow', files: single(loopsAndControl) },
];
