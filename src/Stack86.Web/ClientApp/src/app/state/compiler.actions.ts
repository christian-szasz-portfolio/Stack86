import { createActionGroup, emptyProps, props } from '@ngrx/store';
import { CompilationDiagnostic, CompilerFile, SupportedLanguage } from '../core/compiler/compiler.models';

export const CompilerActions = createActionGroup({
  source: 'Compiler',
  events: {
    'Add File': props<{ file: CompilerFile }>(),
    'Remove File': props<{ fileId: string }>(),
    'Rename File': props<{ fileId: string; name: string }>(),
    'Set Active File': props<{ fileId: string }>(),
    'Update File Content': props<{ fileId: string; content: string }>(),
    'Load Project': props<{ files: CompilerFile[] }>(),
    'Select Language': props<{ language: SupportedLanguage }>(),
    'Compile': emptyProps(),
    'Compile Log': props<{ text: string }>(),
    'Compile Success': props<{
      assembly: string | null;
      errors: CompilationDiagnostic[];
      warnings: CompilationDiagnostic[];
    }>(),
    'Compile Failure': props<{ error: string }>(),
    'Compile Blocked': props<{ remainingCooldownMs: number }>(),
    'Load Assembly Into Assembler': emptyProps(),
  },
});
