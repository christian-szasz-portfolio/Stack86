import { provideStore } from '@ngrx/store';
import { provideEffects } from '@ngrx/effects';
import { Subject } from 'rxjs';
import { vi, type MockInstance } from 'vitest';
import { COMPILER_FEATURE_KEY } from '../state/compiler.state';
import { compilerReducer } from '../state/compiler.reducer';
import { EMULATOR_FEATURE_KEY } from '../state/emulator.state';
import { emulatorReducer } from '../state/emulator.reducer';

/**
 * Test helpers for Stack86 Vitest specs. Keep this file framework-agnostic where possible —
 * each helper is a static class method or top-level function so tests stay declarative.
 */

/** Provides the same compiler+emulator stores tests use, with no real effects. */
export function provideTestStores() {
  return [
    provideStore({
      [COMPILER_FEATURE_KEY]: compilerReducer,
      [EMULATOR_FEATURE_KEY]: emulatorReducer,
    }),
    provideEffects(),
  ];
}

/** Minimal monaco editor instance shape used by Stack86. */
export interface MockMonacoEditor {
  getValue: ReturnType<typeof vi.fn>;
  setValue: ReturnType<typeof vi.fn>;
  onDidChangeModelContent: ReturnType<typeof vi.fn>;
  onDidFocusEditorWidget: ReturnType<typeof vi.fn>;
  onDidBlurEditorWidget: ReturnType<typeof vi.fn>;
  dispose: ReturnType<typeof vi.fn>;
  layout: ReturnType<typeof vi.fn>;
  getModel: ReturnType<typeof vi.fn>;
  setModel: ReturnType<typeof vi.fn>;
  updateOptions: ReturnType<typeof vi.fn>;
  getPosition: ReturnType<typeof vi.fn>;
  setPosition: ReturnType<typeof vi.fn>;
  focus: ReturnType<typeof vi.fn>;
  trigger: ReturnType<typeof vi.fn>;
  addAction: ReturnType<typeof vi.fn>;
  deltaDecorations: ReturnType<typeof vi.fn>;
  revealLine: ReturnType<typeof vi.fn>;
  revealLineInCenter: ReturnType<typeof vi.fn>;
}

export class MonacoTestUtility {
  public static createEditor(initialValue: string = ''): MockMonacoEditor {
    let buffer = initialValue;
    const noopDisposable = { dispose: vi.fn() };
    return {
      getValue: vi.fn(() => buffer),
      setValue: vi.fn((value: string) => {
        buffer = value;
      }),
      onDidChangeModelContent: vi.fn(() => noopDisposable),
      onDidFocusEditorWidget: vi.fn(() => noopDisposable),
      onDidBlurEditorWidget: vi.fn(() => noopDisposable),
      dispose: vi.fn(),
      layout: vi.fn(),
      getModel: vi.fn(() => ({
        getValue: () => buffer,
        setValue: (value: string) => {
          buffer = value;
        },
        getLineCount: () => buffer.split('\n').length,
        uri: { toString: () => 'inmemory://test' },
        dispose: vi.fn(),
        onDidChangeContent: vi.fn(() => noopDisposable),
      })),
      setModel: vi.fn(),
      updateOptions: vi.fn(),
      getPosition: vi.fn(() => ({ lineNumber: 1, column: 1 })),
      setPosition: vi.fn(),
      focus: vi.fn(),
      trigger: vi.fn(),
      addAction: vi.fn(() => noopDisposable),
      deltaDecorations: vi.fn(() => []),
      revealLine: vi.fn(),
      revealLineInCenter: vi.fn(),
    };
  }
}

/** Minimal CanvasRenderingContext2D shape covered with vi.fn spies. */
export interface MockCanvasContext {
  fillRect: ReturnType<typeof vi.fn>;
  clearRect: ReturnType<typeof vi.fn>;
  strokeRect: ReturnType<typeof vi.fn>;
  fillText: ReturnType<typeof vi.fn>;
  strokeText: ReturnType<typeof vi.fn>;
  measureText: ReturnType<typeof vi.fn>;
  beginPath: ReturnType<typeof vi.fn>;
  closePath: ReturnType<typeof vi.fn>;
  moveTo: ReturnType<typeof vi.fn>;
  lineTo: ReturnType<typeof vi.fn>;
  quadraticCurveTo: ReturnType<typeof vi.fn>;
  bezierCurveTo: ReturnType<typeof vi.fn>;
  arc: ReturnType<typeof vi.fn>;
  rect: ReturnType<typeof vi.fn>;
  stroke: ReturnType<typeof vi.fn>;
  fill: ReturnType<typeof vi.fn>;
  createLinearGradient: ReturnType<typeof vi.fn>;
  createRadialGradient: ReturnType<typeof vi.fn>;
  save: ReturnType<typeof vi.fn>;
  restore: ReturnType<typeof vi.fn>;
  translate: ReturnType<typeof vi.fn>;
  scale: ReturnType<typeof vi.fn>;
  rotate: ReturnType<typeof vi.fn>;
  setTransform: ReturnType<typeof vi.fn>;
  resetTransform: ReturnType<typeof vi.fn>;
  setLineDash: ReturnType<typeof vi.fn>;
  fillStyle: string;
  strokeStyle: string;
  lineWidth: number;
  font: string;
  textAlign: string;
  textBaseline: string;
  globalAlpha: number;
}

export class CanvasTestUtility {
  public static createContext(): MockCanvasContext {
    return {
      fillRect: vi.fn(),
      clearRect: vi.fn(),
      strokeRect: vi.fn(),
      fillText: vi.fn(),
      strokeText: vi.fn(),
      measureText: vi.fn((text: string) => ({ width: text.length * 7 })),
      beginPath: vi.fn(),
      closePath: vi.fn(),
      moveTo: vi.fn(),
      lineTo: vi.fn(),
      quadraticCurveTo: vi.fn(),
      bezierCurveTo: vi.fn(),
      arc: vi.fn(),
      rect: vi.fn(),
      stroke: vi.fn(),
      fill: vi.fn(),
      createLinearGradient: vi.fn(() => ({ addColorStop: vi.fn() })),
      createRadialGradient: vi.fn(() => ({ addColorStop: vi.fn() })),
      save: vi.fn(),
      restore: vi.fn(),
      translate: vi.fn(),
      scale: vi.fn(),
      rotate: vi.fn(),
      setTransform: vi.fn(),
      resetTransform: vi.fn(),
      setLineDash: vi.fn(),
      fillStyle: '',
      strokeStyle: '',
      lineWidth: 1,
      font: '',
      textAlign: '',
      textBaseline: '',
      globalAlpha: 1,
    };
  }

  /** Adds a missing prototype method to MockCanvasContext at runtime if needed by future tests. */
  /** Patches HTMLCanvasElement.prototype.getContext to return a fresh mock. Returns the spy. */
  public static patchPrototype(context: MockCanvasContext): MockInstance {
    return vi
      .spyOn(HTMLCanvasElement.prototype, 'getContext')
      .mockImplementation(() => context as unknown as CanvasRenderingContext2D);
  }
}

/** Generic streaming Subject helper for HTTP NDJSON tests. */
export class StreamTestUtility {
  public static create<T>(): Subject<T> {
    return new Subject<T>();
  }

  public static emitAll<T>(subject: Subject<T>, events: readonly T[]): void {
    for (const event of events) {
      subject.next(event);
    }
    subject.complete();
  }
}
