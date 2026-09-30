import { Service, inject } from '@angular/core';
import { Store } from '@ngrx/store';
import { CompilerActions } from './compiler.actions';
import {
  selectSelectedLanguage,
  selectFiles,
  selectActiveFileId,
  selectActiveFile,
  selectLoading,
  selectAssembly,
  selectErrors,
  selectWarnings,
  selectCompilationError,
  selectCanLoadIntoAssembler,
  selectConsoleMessages,
} from './compiler.selectors';
import { CompilerFile, SupportedLanguage } from '../core/compiler/compiler.models';

@Service()
export class CompilerFacade {
  private readonly store = inject(Store);

  public readonly selectedLanguage = this.store.selectSignal(selectSelectedLanguage);
  public readonly files = this.store.selectSignal(selectFiles);
  public readonly activeFileId = this.store.selectSignal(selectActiveFileId);
  public readonly activeFile = this.store.selectSignal(selectActiveFile);
  public readonly loading = this.store.selectSignal(selectLoading);
  public readonly assembly = this.store.selectSignal(selectAssembly);
  public readonly errors = this.store.selectSignal(selectErrors);
  public readonly warnings = this.store.selectSignal(selectWarnings);
  public readonly compilationError = this.store.selectSignal(selectCompilationError);
  public readonly consoleMessages = this.store.selectSignal(selectConsoleMessages);
  public readonly canLoadIntoAssembler = this.store.selectSignal(selectCanLoadIntoAssembler);

  public addFile(file: CompilerFile): void {
    this.store.dispatch(CompilerActions.addFile({ file }));
  }

  public removeFile(fileId: string): void {
    this.store.dispatch(CompilerActions.removeFile({ fileId }));
  }

  public renameFile(fileId: string, name: string): void {
    this.store.dispatch(CompilerActions.renameFile({ fileId, name }));
  }

  public setActiveFile(fileId: string): void {
    this.store.dispatch(CompilerActions.setActiveFile({ fileId }));
  }

  public updateFileContent(fileId: string, content: string): void {
    this.store.dispatch(CompilerActions.updateFileContent({ fileId, content }));
  }

  public loadProject(files: CompilerFile[]): void {
    this.store.dispatch(CompilerActions.loadProject({ files }));
  }

  public selectLanguage(language: SupportedLanguage): void {
    this.store.dispatch(CompilerActions.selectLanguage({ language }));
  }

  public compile(): void {
    this.store.dispatch(CompilerActions.compile());
  }

  public loadAssemblyIntoAssembler(): void {
    this.store.dispatch(CompilerActions.loadAssemblyIntoAssembler());
  }
}
