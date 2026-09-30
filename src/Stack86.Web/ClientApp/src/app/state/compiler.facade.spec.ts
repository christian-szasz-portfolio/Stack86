import { TestBed } from '@angular/core/testing';
import { provideStore, Store } from '@ngrx/store';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { CompilerFacade } from './compiler.facade';
import { CompilerActions } from './compiler.actions';
import { compilerReducer } from './compiler.reducer';
import { initialCompilerState } from './compiler.state';
import { CompilerFile, SupportedLanguage } from '../core/compiler/compiler.models';

describe('CompilerFacade', () => {
  let facade: CompilerFacade;
  let store: Store;
  let dispatch: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideStore(
          { compiler: compilerReducer },
          { initialState: { compiler: initialCompilerState } },
        ),
      ],
    });
    facade = TestBed.inject(CompilerFacade);
    store = TestBed.inject(Store);
    dispatch = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);
  });

  afterEach(() => vi.restoreAllMocks());

  it('exposes signals reflecting initial state', () => {
    expect(facade.selectedLanguage()).toBe(initialCompilerState.selectedLanguage);
    expect(facade.loading()).toBe(false);
    expect(facade.assembly()).toBeNull();
  });

  it('addFile dispatches Add File action', () => {
    const file: CompilerFile = { id: '1', name: 'a.c', content: '', isMain: false };
    facade.addFile(file);
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.addFile({ file }));
  });

  it('removeFile dispatches Remove File', () => {
    facade.removeFile('1');
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.removeFile({ fileId: '1' }));
  });

  it('renameFile dispatches Rename File', () => {
    facade.renameFile('1', 'b.c');
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.renameFile({ fileId: '1', name: 'b.c' }));
  });

  it('setActiveFile dispatches Set Active File', () => {
    facade.setActiveFile('1');
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.setActiveFile({ fileId: '1' }));
  });

  it('updateFileContent dispatches Update File Content', () => {
    facade.updateFileContent('1', 'src');
    expect(dispatch).toHaveBeenCalledWith(
      CompilerActions.updateFileContent({ fileId: '1', content: 'src' }),
    );
  });

  it('loadProject dispatches Load Project', () => {
    const files: CompilerFile[] = [];
    facade.loadProject(files);
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.loadProject({ files }));
  });

  it('selectLanguage dispatches Select Language', () => {
    facade.selectLanguage(SupportedLanguage.TypeScript);
    expect(dispatch).toHaveBeenCalledWith(
      CompilerActions.selectLanguage({ language: SupportedLanguage.TypeScript }),
    );
  });

  it('compile dispatches Compile', () => {
    facade.compile();
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.compile());
  });

  it('loadAssemblyIntoAssembler dispatches the corresponding action', () => {
    facade.loadAssemblyIntoAssembler();
    expect(dispatch).toHaveBeenCalledWith(CompilerActions.loadAssemblyIntoAssembler());
  });
});
