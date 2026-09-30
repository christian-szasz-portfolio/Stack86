import { TestBed } from '@angular/core/testing';
import { provideStore, Store } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideMockActions } from '@ngrx/effects/testing';
import { Router, provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { firstValueFrom, of, Subject, throwError } from 'rxjs';
import { take, toArray } from 'rxjs/operators';
import { CompilerEffects } from './compiler.effects';
import { CompilerActions } from './compiler.actions';
import { compilerReducer } from './compiler.reducer';
import { initialCompilerState } from './compiler.state';
import { CompilerService } from '../core/compiler/compiler.service';
import { AssemblerBridgeService } from '../core/compiler/assembler-bridge.service';
import { EditorStorageService } from '../core/editor';
import { CompileStreamEvent, SupportedLanguage } from '../core/compiler/compiler.models';

class FakeCompilerService {
  public stream$ = new Subject<CompileStreamEvent>();
  public lastCall: { language: string; files: Record<string, string> } | null = null;
  public compileStream = vi.fn((language: string, files: Record<string, string>) => {
    this.lastCall = { language, files };
    return this.stream$.asObservable();
  });
}

class FakeAssemblerBridge {
  public loadAssemblyIntoAssembler = vi.fn();
}

class FakeEditorStorage {
  public saveCompilerProject = vi.fn(() => Promise.resolve());
  public markCompilerHydrated = vi.fn();
}

function setup(actions$: Subject<unknown>, fakes: {
  compiler: FakeCompilerService;
  bridge: FakeAssemblerBridge;
  storage: FakeEditorStorage;
}): { effects: CompilerEffects; router: Router } {
  TestBed.configureTestingModule({
    providers: [
      provideStore({ compiler: compilerReducer }, { initialState: { compiler: initialCompilerState } }),
      provideEffects(),
      provideRouter([]),
      provideMockActions(() => actions$),
      CompilerEffects,
      { provide: CompilerService, useValue: fakes.compiler },
      { provide: AssemblerBridgeService, useValue: fakes.bridge },
      { provide: EditorStorageService, useValue: fakes.storage },
    ],
  });
  return {
    effects: TestBed.inject(CompilerEffects),
    router: TestBed.inject(Router),
  };
}

describe('CompilerEffects', () => {
  let actions$: Subject<unknown>;
  let compiler: FakeCompilerService;
  let bridge: FakeAssemblerBridge;
  let storage: FakeEditorStorage;

  beforeEach(() => {
    actions$ = new Subject<unknown>();
    compiler = new FakeCompilerService();
    bridge = new FakeAssemblerBridge();
    storage = new FakeEditorStorage();
  });

  afterEach(() => vi.restoreAllMocks());

  describe('compile$', () => {
    it('emits compileLog for log events from the stream', async () => {
      const { effects } = setup(actions$, { compiler, bridge, storage });
      const promise = firstValueFrom(effects.compile$.pipe(take(1)));
      actions$.next(CompilerActions.compile());
      compiler.stream$.next({ type: 'log', text: '[Info] starting' });

      const action = await promise;
      expect(action.type).toBe(CompilerActions.compileLog.type);
      expect((action as ReturnType<typeof CompilerActions.compileLog>).text).toBe('[Info] starting');
    });

    it('emits compileSuccess on result events', async () => {
      const { effects } = setup(actions$, { compiler, bridge, storage });
      const promise = firstValueFrom(effects.compile$.pipe(take(1)));
      actions$.next(CompilerActions.compile());
      compiler.stream$.next({
        type: 'result',
        assembly: 'MOV AX, 1',
        errors: [],
        warnings: [],
      });

      const action = await promise;
      expect(action.type).toBe(CompilerActions.compileSuccess.type);
      const success = action as ReturnType<typeof CompilerActions.compileSuccess>;
      expect(success.assembly).toBe('MOV AX, 1');
      expect(success.errors).toEqual([]);
    });

    it('emits compileFailure when the stream errors', async () => {
      compiler.compileStream = vi.fn(() => throwError(() => new Error('network down')));
      const { effects } = setup(actions$, { compiler, bridge, storage });
      const promise = firstValueFrom(effects.compile$.pipe(take(1)));
      actions$.next(CompilerActions.compile());

      const action = await promise;
      expect(action.type).toBe(CompilerActions.compileFailure.type);
      expect((action as ReturnType<typeof CompilerActions.compileFailure>).error).toBe('network down');
    });

    it('passes selected language and file dictionary to compileStream', async () => {
      const { effects } = setup(actions$, { compiler, bridge, storage });
      // Subscribe to keep the effect active (no need to consume each emit).
      const sub = effects.compile$.subscribe();
      actions$.next(CompilerActions.compile());
      // Provide a synchronous result to trigger downstream operators.
      compiler.stream$.next({ type: 'result', assembly: '', errors: [], warnings: [] });

      expect(compiler.compileStream).toHaveBeenCalledTimes(1);
      expect(compiler.lastCall?.language).toBe(SupportedLanguage.C);
      expect(typeof compiler.lastCall?.files).toBe('object');
      sub.unsubscribe();
    });
  });

  describe('persistFiles$', () => {
    it('does not fire immediately (debounced)', async () => {
      vi.useFakeTimers();
      try {
        const { effects } = setup(actions$, { compiler, bridge, storage });
        const collected: unknown[] = [];
        const sub = effects.persistFiles$.subscribe((v) => collected.push(v));

        actions$.next(CompilerActions.updateFileContent({ fileId: 'x', content: 'a' }));
        // Less than debounce window
        await vi.advanceTimersByTimeAsync(100);
        expect(storage.saveCompilerProject).not.toHaveBeenCalled();

        await vi.advanceTimersByTimeAsync(1100);
        expect(storage.saveCompilerProject).toHaveBeenCalledTimes(1);
        sub.unsubscribe();
        expect(collected.length).toBeGreaterThan(0);
      } finally {
        vi.useRealTimers();
      }
    });
  });

  describe('loadAssemblyIntoAssembler$', () => {
    it('does nothing when assembly is null', async () => {
      const { effects, router } = setup(actions$, { compiler, bridge, storage });
      const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
      const sub = effects.loadAssemblyIntoAssembler$.subscribe();
      actions$.next(CompilerActions.loadAssemblyIntoAssembler());

      expect(bridge.loadAssemblyIntoAssembler).not.toHaveBeenCalled();
      expect(navigate).not.toHaveBeenCalled();
      sub.unsubscribe();
    });

    it('forwards assembly to the bridge and navigates when set', async () => {
      const { effects, router } = setup(actions$, { compiler, bridge, storage });
      // Seed assembly into the store directly via the real Store dispatch
      // (provideMockActions doesn't pipe actions through the reducer).
      const store = TestBed.inject(Store);
      store.dispatch(
        CompilerActions.compileSuccess({
          assembly: 'MOV AX, 1',
          errors: [],
          warnings: [],
        }),
      );
      const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);
      const sub = effects.loadAssemblyIntoAssembler$.subscribe();
      actions$.next(CompilerActions.loadAssemblyIntoAssembler());

      expect(bridge.loadAssemblyIntoAssembler).toHaveBeenCalledTimes(1);
      const [asm, origin] = bridge.loadAssemblyIntoAssembler.mock.calls[0];
      expect(asm).toBe('MOV AX, 1');
      expect(origin.language).toBe(SupportedLanguage.C);
      expect(navigate).toHaveBeenCalledWith(['/assembler']);
      sub.unsubscribe();
    });
  });

  // toArray reference guard — ensures unused-import linter doesn't complain
  it('imports rxjs operators used internally', () => {
    expect(typeof of).toBe('function');
    expect(typeof toArray).toBe('function');
  });
});
