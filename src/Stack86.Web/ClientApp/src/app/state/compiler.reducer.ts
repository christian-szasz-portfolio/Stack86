import { createReducer, on } from '@ngrx/store';
import { CompilerActions } from './compiler.actions';
import { CompilerState, createInitialFiles, initialCompilerState } from './compiler.state';

export const compilerReducer = createReducer(
  initialCompilerState,

  on(CompilerActions.addFile, (state, { file }): CompilerState => ({
    ...state,
    files: [...state.files, file],
    activeFileId: file.id,
  })),

  on(CompilerActions.removeFile, (state, { fileId }): CompilerState => {
    const target = state.files.find((f) => f.id === fileId);
    if (!target || target.isMain || state.files.length <= 1) {
      return state;
    }
    const remaining = state.files.filter((f) => f.id !== fileId);
    const newActiveId = state.activeFileId === fileId ? remaining[0].id : state.activeFileId;
    return { ...state, files: remaining, activeFileId: newActiveId };
  }),

  on(CompilerActions.renameFile, (state, { fileId, name }): CompilerState => ({
    ...state,
    files: state.files.map((f) => (f.id === fileId ? { ...f, name } : f)),
  })),

  on(CompilerActions.setActiveFile, (state, { fileId }): CompilerState => ({
    ...state,
    activeFileId: fileId,
  })),

  on(CompilerActions.updateFileContent, (state, { fileId, content }): CompilerState => ({
    ...state,
    files: state.files.map((f) => (f.id === fileId ? { ...f, content } : f)),
  })),

  on(CompilerActions.loadProject, (state, { files }): CompilerState => {
    const mainFile = files.find((f) => f.isMain) ?? files[0];
    return {
      ...state,
      files,
      activeFileId: mainFile.id,
      assembly: null,
      errors: [],
      warnings: [],
      compilationError: null,
      consoleMessages: [],
    };
  }),

  on(CompilerActions.selectLanguage, (state, { language }): CompilerState => {
    const files = createInitialFiles(language);
    return {
      ...state,
      selectedLanguage: language,
      files,
      activeFileId: files[0].id,
      assembly: null,
      errors: [],
      warnings: [],
      compilationError: null,
      consoleMessages: [],
    };
  }),

  on(CompilerActions.compile, (state): CompilerState => ({
    ...state,
    loading: true,
    assembly: null,
    errors: [],
    warnings: [],
    compilationError: null,
    consoleMessages: [],
    circuitOpen: false,
    circuitCooldownMs: 0,
  })),

  on(CompilerActions.compileLog, (state, { text }): CompilerState => ({
    ...state,
    consoleMessages: [...state.consoleMessages, text],
  })),

  on(CompilerActions.compileSuccess, (state, { assembly, errors, warnings }): CompilerState => ({
    ...state,
    loading: false,
    assembly,
    errors,
    warnings,
  })),

  on(CompilerActions.compileFailure, (state, { error }): CompilerState => ({
    ...state,
    loading: false,
    compilationError: error,
  })),

  on(CompilerActions.compileBlocked, (state, { remainingCooldownMs }): CompilerState => ({
    ...state,
    loading: false,
    compilationError: 'Compiler temporarily unavailable. Please retry shortly.',
    circuitOpen: true,
    circuitCooldownMs: remainingCooldownMs,
  })),
);
