import { createFeatureSelector, createSelector } from '@ngrx/store';
import { CompilerState, COMPILER_FEATURE_KEY } from './compiler.state';

export const selectCompilerState = createFeatureSelector<CompilerState>(COMPILER_FEATURE_KEY);

export const selectSelectedLanguage = createSelector(selectCompilerState, (state) => state.selectedLanguage);
export const selectFiles = createSelector(selectCompilerState, (state) => state.files);
export const selectActiveFileId = createSelector(selectCompilerState, (state) => state.activeFileId);
export const selectActiveFile = createSelector(selectFiles, selectActiveFileId, (files, id) => files.find((f) => f.id === id) ?? files[0]);
export const selectLoading = createSelector(selectCompilerState, (state) => state.loading);
export const selectAssembly = createSelector(selectCompilerState, (state) => state.assembly);
export const selectErrors = createSelector(selectCompilerState, (state) => state.errors);
export const selectWarnings = createSelector(selectCompilerState, (state) => state.warnings);
export const selectCompilationError = createSelector(selectCompilerState, (state) => state.compilationError);
export const selectConsoleMessages = createSelector(selectCompilerState, (state) => state.consoleMessages);

export const selectAllFilesAsDict = createSelector(selectFiles, (files) => {
  const dict: Record<string, string> = {};
  for (const f of files) {
    dict[f.name] = f.content;
  }
  return dict;
});

export const selectHasErrors = createSelector(
  selectErrors,
  selectCompilationError,
  (errors, compilationError) => errors.length > 0 || compilationError !== null,
);

export const selectCanLoadIntoAssembler = createSelector(
  selectAssembly,
  selectLoading,
  (assembly, loading) => assembly !== null && !loading,
);
