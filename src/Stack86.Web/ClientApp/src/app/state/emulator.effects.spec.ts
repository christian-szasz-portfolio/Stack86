import { TestBed } from '@angular/core/testing';
import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { provideMockActions } from '@ngrx/effects/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { firstValueFrom, Subject } from 'rxjs';
import { take } from 'rxjs/operators';
import { EmulatorEffects } from './emulator.effects';
import { EmulatorActions } from './emulator.actions';
import { emulatorReducer } from './emulator.reducer';
import { initialEmulatorState } from './emulator.state';
import { EmulatorService } from './emulator.service';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { ExecutionEngine } from '../core/emulator/execution/execution-engine';
import { EditorStorageService } from '../core/editor';
import { ComFileService } from '../core/emulator/binary/com-file.service';

class FakeEditorStorage {
  public saveAssemblerSource = vi.fn();
}

class FakeComFileService {
  public exportCom = vi.fn();
  public importCom = vi.fn(() => Promise.resolve({ data: new Uint8Array(), filename: 'x.com' }));
}

function setup(actions$: Subject<unknown>, fakes: {
  storage: FakeEditorStorage;
  com: FakeComFileService;
}): { effects: EmulatorEffects; engine: ExecutionEngine } {
  TestBed.configureTestingModule({
    providers: [
      provideStore({ emulator: emulatorReducer }, { initialState: { emulator: initialEmulatorState } }),
      provideEffects(),
      provideMockActions(() => actions$),
      EmulatorEffects,
      { provide: EditorStorageService, useValue: fakes.storage },
      { provide: ComFileService, useValue: fakes.com },
    ],
  });
  const effects = TestBed.inject(EmulatorEffects);
  const engine = TestBed.inject(EmulatorService).engine;
  return { effects, engine };
}

describe('EmulatorEffects', () => {
  let actions$: Subject<unknown>;
  let storage: FakeEditorStorage;
  let com: FakeComFileService;

  beforeEach(() => {
    actions$ = new Subject<unknown>();
    storage = new FakeEditorStorage();
    com = new FakeComFileService();
  });

  afterEach(() => vi.restoreAllMocks());

  describe('loadProgram$', () => {
    it('emits loadProgramSuccess for valid assembly', async () => {
      const { effects } = setup(actions$, { storage, com });
      const promise = firstValueFrom(effects.loadProgram$.pipe(take(1)));
      actions$.next(EmulatorActions.loadProgram({ source: 'MOV AX, 5' }));
      const action = await promise;
      expect(action.type).toBe(EmulatorActions.loadProgramSuccess.type);
    });

    it('emits loadProgramFailure for invalid assembly', async () => {
      const { effects } = setup(actions$, { storage, com });
      const promise = firstValueFrom(effects.loadProgram$.pipe(take(1)));
      actions$.next(EmulatorActions.loadProgram({ source: 'BOGUS_OPCODE' }));
      const action = await promise;
      expect(action.type).toBe(EmulatorActions.loadProgramFailure.type);
    });
  });

  describe('step$', () => {
    it('emits stepSuccess after stepping a loaded program', async () => {
      const { effects, engine } = setup(actions$, { storage, com });
      engine.loadProgram('MOV AX, 7\nHLT');
      const promise = firstValueFrom(effects.step$.pipe(take(1)));
      actions$.next(EmulatorActions.step());
      const action = await promise;
      expect(action.type).toBe(EmulatorActions.stepSuccess.type);
      const stepAction = action as ReturnType<typeof EmulatorActions.stepSuccess>;
      expect(stepAction.cpu.ax).toBe(7);
      expect(stepAction.status).toBe(ExecutionStatus.Paused);
    });
  });

  describe('pause$ / reset$ / breakpoints', () => {
    it('pause delegates to engine.pause', () => {
      const { effects, engine } = setup(actions$, { storage, com });
      const spy = vi.spyOn(engine, 'pause');
      const sub = effects.pause$.subscribe();
      actions$.next(EmulatorActions.pause());
      expect(spy).toHaveBeenCalled();
      sub.unsubscribe();
    });

    it('reset delegates to engine.reset', () => {
      const { effects, engine } = setup(actions$, { storage, com });
      const spy = vi.spyOn(engine, 'reset');
      const sub = effects.reset$.subscribe();
      actions$.next(EmulatorActions.reset());
      expect(spy).toHaveBeenCalled();
      sub.unsubscribe();
    });

    it('setBreakpoint delegates to engine.setBreakpoint', () => {
      const { effects, engine } = setup(actions$, { storage, com });
      const spy = vi.spyOn(engine, 'setBreakpoint');
      const sub = effects.setBreakpoint$.subscribe();
      actions$.next(EmulatorActions.setBreakpoint({ address: 7 }));
      expect(spy).toHaveBeenCalledWith(7);
      sub.unsubscribe();
    });

    it('removeBreakpoint delegates to engine.removeBreakpoint', () => {
      const { effects, engine } = setup(actions$, { storage, com });
      const spy = vi.spyOn(engine, 'removeBreakpoint');
      const sub = effects.removeBreakpoint$.subscribe();
      actions$.next(EmulatorActions.removeBreakpoint({ address: 7 }));
      expect(spy).toHaveBeenCalledWith(7);
      sub.unsubscribe();
    });
  });

  describe('persistSourceCode$', () => {
    it('saves source after debounce', async () => {
      vi.useFakeTimers();
      try {
        const { effects } = setup(actions$, { storage, com });
        const sub = effects.persistSourceCode$.subscribe();
        actions$.next(EmulatorActions.updateSourceCode({ source: 'NOP' }));
        await vi.advanceTimersByTimeAsync(100);
        expect(storage.saveAssemblerSource).not.toHaveBeenCalled();
        await vi.advanceTimersByTimeAsync(1100);
        expect(storage.saveAssemblerSource).toHaveBeenCalledWith('NOP');
        sub.unsubscribe();
      } finally {
        vi.useRealTimers();
      }
    });
  });
});
