import { describe, it, expect } from 'vitest';
import {
  selectSelectedLanguage,
  selectFiles,
  selectActiveFile,
  selectAllFilesAsDict,
  selectLoading,
  selectAssembly,
  selectErrors,
  selectWarnings,
  selectCompilationError,
  selectHasErrors,
  selectCanLoadIntoAssembler,
} from './compiler.selectors';
import { CompilerState, initialCompilerState } from './compiler.state';
import { CompilerFile, SupportedLanguage } from '../core/compiler/compiler.models';

function withState(overrides: Partial<CompilerState>): { compiler: CompilerState } {
  return { compiler: { ...initialCompilerState, ...overrides } };
}

describe('Compiler Selectors', () => {
  it('should select selectedLanguage', () => {
    const state = withState({ selectedLanguage: SupportedLanguage.TypeScript });
    expect(selectSelectedLanguage(state)).toBe(SupportedLanguage.TypeScript);
  });

  it('should select files', () => {
    const files: CompilerFile[] = [
      { id: '1', name: 'main.c', content: 'int main() {}', isMain: true },
    ];
    const state = withState({ files });
    expect(selectFiles(state)).toEqual(files);
  });

  it('should select active file', () => {
    const files: CompilerFile[] = [
      { id: '1', name: 'main.c', content: '', isMain: true },
      { id: '2', name: 'utils.c', content: '', isMain: false },
    ];
    const state = withState({ files, activeFileId: '2' });
    expect(selectActiveFile(state)?.id).toBe('2');
  });

  it('should build files dict', () => {
    const files: CompilerFile[] = [
      { id: '1', name: 'main.c', content: 'int main() {}', isMain: true },
      { id: '2', name: 'utils.h', content: 'int add();', isMain: false },
    ];
    const state = withState({ files });
    expect(selectAllFilesAsDict(state)).toEqual({
      'main.c': 'int main() {}',
      'utils.h': 'int add();',
    });
  });

  it('should select loading', () => {
    expect(selectLoading(withState({ loading: false }))).toBe(false);
    expect(selectLoading(withState({ loading: true }))).toBe(true);
  });

  it('should select assembly', () => {
    expect(selectAssembly(withState({ assembly: null }))).toBeNull();
    expect(selectAssembly(withState({ assembly: 'MOV AX, 1' }))).toBe('MOV AX, 1');
  });

  it('should select errors', () => {
    const errors = [{ message: 'err', line: 1, column: 1 }];
    expect(selectErrors(withState({ errors }))).toEqual(errors);
  });

  it('should select warnings', () => {
    const warnings = [{ message: 'warn', line: 2, column: 3 }];
    expect(selectWarnings(withState({ warnings }))).toEqual(warnings);
  });

  it('should select compilationError', () => {
    expect(selectCompilationError(withState({}))).toBeNull();
    expect(selectCompilationError(withState({ compilationError: 'fail' }))).toBe('fail');
  });

  describe('selectHasErrors', () => {
    it('should return false when no errors and no compilation error', () => {
      expect(selectHasErrors(withState({}))).toBe(false);
    });

    it('should return true when there are diagnostic errors', () => {
      const state = withState({ errors: [{ message: 'err', line: 1, column: 1 }] });
      expect(selectHasErrors(state)).toBe(true);
    });

    it('should return true when there is a compilation error', () => {
      const state = withState({ compilationError: 'network error' });
      expect(selectHasErrors(state)).toBe(true);
    });
  });

  describe('selectCanLoadIntoAssembler', () => {
    it('should return false when no assembly', () => {
      expect(selectCanLoadIntoAssembler(withState({}))).toBe(false);
    });

    it('should return false when loading', () => {
      const state = withState({ assembly: 'MOV AX, 1', loading: true });
      expect(selectCanLoadIntoAssembler(state)).toBe(false);
    });

    it('should return true when assembly exists and not loading', () => {
      const state = withState({ assembly: 'MOV AX, 1', loading: false });
      expect(selectCanLoadIntoAssembler(state)).toBe(true);
    });
  });
});
