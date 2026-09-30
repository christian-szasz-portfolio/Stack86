import helloArithmetic from './samples/csharp/hello-arithmetic.cs?raw';
import classesAndObjects from './samples/csharp/classes-and-objects.cs?raw';
import inheritance from './samples/csharp/inheritance.cs?raw';
import enumsAndSwitch from './samples/csharp/enums-and-switch.cs?raw';
import properties from './samples/csharp/properties.cs?raw';
import staticMembers from './samples/csharp/static-members.cs?raw';
import loopsAndControl from './samples/csharp/loops-and-control.cs?raw';
import structs from './samples/csharp/structs.cs?raw';
import { SampleProgram } from './sample-programs';

function single(source: string): { name: string; content: string }[] {
  return [{ name: 'main.cs', content: source }];
}

export const CSHARP_SAMPLE_PROGRAMS: SampleProgram[] = [
  { name: 'Hello Arithmetic', files: single(helloArithmetic) },
  { name: 'Classes & Objects', files: single(classesAndObjects) },
  { name: 'Inheritance', files: single(inheritance) },
  { name: 'Enums & Switch', files: single(enumsAndSwitch) },
  { name: 'Properties', files: single(properties) },
  { name: 'Static Members', files: single(staticMembers) },
  { name: 'Loops & Control Flow', files: single(loopsAndControl) },
  { name: 'Structs', files: single(structs) },
];
