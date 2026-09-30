import { describe, it, expect } from 'vitest';
import { compilerReducer } from './compiler.reducer';
import { CompilerActions } from './compiler.actions';
import { initialCompilerState } from './compiler.state';
import { CompilerFile, SupportedLanguage, createMainFile } from '../core/compiler/compiler.models';

describe('compilerReducer', () => {
  it('should return initial state', () => {
    const state = compilerReducer(undefined, { type: '[Init]' });
    expect(state).toEqual(initialCompilerState);
  });

  it('should have C as default language', () => {
    expect(initialCompilerState.selectedLanguage).toBe(SupportedLanguage.C);
  });

  it('should add a file', () => {
    const file: CompilerFile = { id: 'f1', name: 'utils.c', content: 'int x = 1;', isMain: false };
    const state = compilerReducer(
      initialCompilerState,
      CompilerActions.addFile({ file }),
    );
    expect(state.files).toHaveLength(2);
    expect(state.activeFileId).toBe('f1');
  });

  it('should remove a non-main file', () => {
    const file: CompilerFile = { id: 'f1', name: 'utils.c', content: '', isMain: false };
    const withFile = compilerReducer(initialCompilerState, CompilerActions.addFile({ file }));
    const state = compilerReducer(withFile, CompilerActions.removeFile({ fileId: 'f1' }));
    expect(state.files).toHaveLength(1);
    expect(state.files[0].isMain).toBe(true);
  });

  it('should not remove the main file', () => {
    const mainId = initialCompilerState.files[0].id;
    const state = compilerReducer(initialCompilerState, CompilerActions.removeFile({ fileId: mainId }));
    expect(state.files).toHaveLength(1);
  });

  it('should update file content', () => {
    const mainId = initialCompilerState.files[0].id;
    const state = compilerReducer(
      initialCompilerState,
      CompilerActions.updateFileContent({ fileId: mainId, content: 'int x = 1;' }),
    );
    expect(state.files[0].content).toBe('int x = 1;');
  });

  it('should set active file', () => {
    const file: CompilerFile = { id: 'f1', name: 'utils.c', content: '', isMain: false };
    const withFile = compilerReducer(initialCompilerState, CompilerActions.addFile({ file }));
    const mainId = initialCompilerState.files[0].id;
    const state = compilerReducer(withFile, CompilerActions.setActiveFile({ fileId: mainId }));
    expect(state.activeFileId).toBe(mainId);
  });

  it('should load project', () => {
    const files = [
      createMainFile(SupportedLanguage.C, 'main'),
      { id: 'f2', name: 'utils.h', content: 'header', isMain: false },
    ];
    const state = compilerReducer(initialCompilerState, CompilerActions.loadProject({ files }));
    expect(state.files).toHaveLength(2);
    expect(state.activeFileId).toBe(files[0].id);
    expect(state.assembly).toBeNull();
  });

  it('should select language and reset files', () => {
    const state = compilerReducer(
      initialCompilerState,
      CompilerActions.selectLanguage({ language: SupportedLanguage.Cpp }),
    );
    expect(state.selectedLanguage).toBe(SupportedLanguage.Cpp);
    expect(state.files).toHaveLength(1);
    expect(state.files[0].isMain).toBe(true);
    expect(state.files[0].name).toBe('main.cpp');
    expect(state.assembly).toBeNull();
    expect(state.errors).toEqual([]);
    expect(state.warnings).toEqual([]);
  });

  it('should set loading on compile', () => {
    const state = compilerReducer(initialCompilerState, CompilerActions.compile());
    expect(state.loading).toBe(true);
    expect(state.assembly).toBeNull();
    expect(state.errors).toEqual([]);
    expect(state.warnings).toEqual([]);
    expect(state.compilationError).toBeNull();
  });

  it('should clear previous results on compile', () => {
    const withResults = {
      ...initialCompilerState,
      assembly: 'MOV AX, 1',
      errors: [{ message: 'err', line: 1, column: 1 }],
      warnings: [{ message: 'warn', line: 2, column: 1 }],
      compilationError: 'old error',
    };
    const state = compilerReducer(withResults, CompilerActions.compile());
    expect(state.assembly).toBeNull();
    expect(state.errors).toEqual([]);
    expect(state.warnings).toEqual([]);
    expect(state.compilationError).toBeNull();
  });

  it('should handle compile success', () => {
    const loading = { ...initialCompilerState, loading: true };
    const state = compilerReducer(
      loading,
      CompilerActions.compileSuccess({
        assembly: 'MOV AX, 5',
        errors: [],
        warnings: [{ message: 'unused', line: 1, column: 1 }],
      }),
    );
    expect(state.loading).toBe(false);
    expect(state.assembly).toBe('MOV AX, 5');
    expect(state.errors).toEqual([]);
    expect(state.warnings).toHaveLength(1);
  });

  it('should append console messages on compile log', () => {
    const loading = { ...initialCompilerState, loading: true };
    const state1 = compilerReducer(loading, CompilerActions.compileLog({ text: '[Info] Step 1' }));
    expect(state1.consoleMessages).toEqual(['[Info] Step 1']);
    const state2 = compilerReducer(state1, CompilerActions.compileLog({ text: '[Info] Step 2' }));
    expect(state2.consoleMessages).toEqual(['[Info] Step 1', '[Info] Step 2']);
  });

  it('should handle compile success with errors and no assembly', () => {
    const loading = { ...initialCompilerState, loading: true };
    const state = compilerReducer(
      loading,
      CompilerActions.compileSuccess({
        assembly: null,
        errors: [{ message: 'syntax error', line: 3, column: 10 }],
        warnings: [],
      }),
    );
    expect(state.loading).toBe(false);
    expect(state.assembly).toBeNull();
    expect(state.errors).toHaveLength(1);
    expect(state.errors[0].message).toBe('syntax error');
  });

  it('should handle compile failure', () => {
    const loading = { ...initialCompilerState, loading: true };
    const state = compilerReducer(
      loading,
      CompilerActions.compileFailure({ error: 'Network error' }),
    );
    expect(state.loading).toBe(false);
    expect(state.compilationError).toBe('Network error');
  });

  it('should not mutate original state', () => {
    const original = { ...initialCompilerState };
    const mainId = initialCompilerState.files[0].id;
    compilerReducer(
      initialCompilerState,
      CompilerActions.updateFileContent({ fileId: mainId, content: 'changed' }),
    );
    expect(initialCompilerState).toEqual(original);
  });
});
