import { TestBed } from '@angular/core/testing';
import { provideStore, Store } from '@ngrx/store';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EmulatorFacade } from './emulator.facade';
import { EmulatorActions } from './emulator.actions';
import { emulatorReducer } from './emulator.reducer';
import { initialEmulatorState } from './emulator.state';

describe('EmulatorFacade', () => {
  let facade: EmulatorFacade;
  let store: Store;
  let dispatch: ReturnType<typeof vi.spyOn>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideStore(
          { emulator: emulatorReducer },
          { initialState: { emulator: initialEmulatorState } },
        ),
      ],
    });
    facade = TestBed.inject(EmulatorFacade);
    store = TestBed.inject(Store);
    dispatch = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);
  });

  afterEach(() => vi.restoreAllMocks());

  it('reads initial signal values from the store', () => {
    expect(facade.executionStatus()).toBe(initialEmulatorState.status);
    expect(facade.error()).toBeNull();
    expect(facade.executionDelayMs()).toBe(0);
  });

  describe('command dispatchers', () => {
    it('loadProgram', () => {
      facade.loadProgram('NOP');
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.loadProgram({ source: 'NOP' }));
    });

    it('step', () => {
      facade.step();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.step());
    });

    it('run', () => {
      facade.run();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.run());
    });

    it('pause', () => {
      facade.pause();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.pause());
    });

    it('reset', () => {
      facade.reset();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.reset());
    });

    it('setBreakpoint', () => {
      facade.setBreakpoint(0x10);
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.setBreakpoint({ address: 0x10 }));
    });

    it('removeBreakpoint', () => {
      facade.removeBreakpoint(0x10);
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.removeBreakpoint({ address: 0x10 }));
    });

    it('updateSourceCode', () => {
      facade.updateSourceCode('SRC');
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.updateSourceCode({ source: 'SRC' }));
    });

    it('exportCom', () => {
      facade.exportCom();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.exportCom());
    });

    it('importCom', () => {
      facade.importCom();
      expect(dispatch).toHaveBeenCalledWith(EmulatorActions.importCom());
    });
  });

  describe('execution delay signal', () => {
    it('setExecutionDelay updates the signal', () => {
      facade.setExecutionDelay(50);
      expect(facade.executionDelayMs()).toBe(50);
    });
  });
});
