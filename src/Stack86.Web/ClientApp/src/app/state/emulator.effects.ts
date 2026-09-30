import { Service, inject } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { EmulatorActions } from './emulator.actions';
import { EmulatorFacade } from './emulator.facade';
import { EmulatorService } from './emulator.service';
import { ExecutionEngine } from '../core/emulator/execution/execution-engine';
import { ExecutionStatus } from '../core/emulator/execution/execution-result.model';
import { debounceTime, map, switchMap, takeUntil, tap } from 'rxjs/operators';
import { Observable, Subscriber } from 'rxjs';
import { EmulatorEffectAction } from './emulator.actions';
import { EditorStorageService } from '../core/editor';
import { X86Encoder } from '../core/emulator/binary/x86-encoder';
import { X86Decoder } from '../core/emulator/binary/x86-decoder';
import { ComFileService } from '../core/emulator/binary/com-file.service';

@Service({ autoProvided: false })
export class EmulatorEffects {
  private readonly actions$ = inject(Actions);
  private readonly facade = inject(EmulatorFacade);
  private readonly emulatorService = inject(EmulatorService);
  private readonly editorStorage = inject(EditorStorageService);
  private readonly comFileService = inject(ComFileService);
  private readonly encoder = new X86Encoder();
  private readonly decoder = new X86Decoder();

  public readonly loadProgram$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EmulatorActions.loadProgram),
      map(({ source }) => {
        const engine = this.emulatorService.engine;
        const lineCount = source.split(/\r?\n/).filter((l) => l.trim().length > 0).length;
        const result = engine.loadProgram(source);

        if (result.errors.length > 0) {
          return EmulatorActions.loadProgramFailure({
            error: result.errors.map((e) => e.toString()).join('\n'),
            parseErrors: result.errors.map((e) => ({
              line: e.line,
              column: e.column,
              message: e.message,
              severity: e.severity,
            })),
          });
        }

        const labels: Record<string, number> = {};
        result.labels.forEach((v, k) => (labels[k] = v));

        const instrCount = result.instructions.length;

        return EmulatorActions.loadProgramSuccess({
          instructions: result.instructions,
          labels,
          memory: Array.from(engine.getMemory().snapshot()),
          logMessages: [
            `[Info] Assembling ${lineCount} lines...`,
            `[Info] Parsed ${instrCount} instructions, ${result.labels.size} labels`,
            result.config.hasModel ? `[Info] Memory model: ${result.config.model}, stack: ${result.config.stackSize} bytes` : '',
            `[Info] Build successful`,
          ].filter((m) => m.length > 0),
        });
      }),
    ),
  );

  public readonly step$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EmulatorActions.step),
      map(() => {
        const engine = this.emulatorService.engine;
        const result = engine.step();

        if (result.status === ExecutionStatus.Error) {
          return EmulatorActions.executionError({ error: engine.getError() ?? 'Unknown error' });
        }

        const halted = result.status === ExecutionStatus.Halted;

        return EmulatorActions.stepSuccess({
          cpu: result.cpu,
          memory: Array.from(result.memorySnapshot),
          status: result.status,
          output: halted ? '[Info] Program halted' : result.output,
          trace: result.trace,
        });
      }),
    ),
  );

  public readonly run$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EmulatorActions.run),
      switchMap(() => {
        const engine = this.emulatorService.engine;
        const delayMs = this.facade.executionDelayMs();
        const stop$ = this.actions$.pipe(ofType(EmulatorActions.pause, EmulatorActions.reset));

        if (delayMs > 0) {
          return this.createAnimatedRun(engine).pipe(takeUntil(stop$));
        }

        return this.createBatchRun(engine).pipe(takeUntil(stop$));
      }),
    ),
  );

  public readonly pause$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.pause),
        tap(() => {
          this.emulatorService.engine.pause();
        }),
      ),
    { dispatch: false },
  );

  public readonly reset$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.reset),
        tap(() => {
          this.emulatorService.engine.reset();
        }),
      ),
    { dispatch: false },
  );

  public readonly setBreakpoint$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.setBreakpoint),
        tap(({ address }) => {
          this.emulatorService.engine.setBreakpoint(address);
        }),
      ),
    { dispatch: false },
  );

  public readonly removeBreakpoint$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.removeBreakpoint),
        tap(({ address }) => {
          this.emulatorService.engine.removeBreakpoint(address);
        }),
      ),
    { dispatch: false },
  );

  public readonly persistSourceCode$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.updateSourceCode),
        debounceTime(1000),
        tap(({ source }) => {
          this.editorStorage.saveAssemblerSource(source);
        }),
      ),
    { dispatch: false },
  );

  public readonly exportCom$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.exportCom),
        tap(() => {
          const source = this.facade.sourceCode();
          const engine = this.emulatorService.engine;
          const result = engine.loadProgram(source);

          if (result.errors.length > 0) {
            return;
          }

          const encoded = this.encoder.encode(result);
          this.comFileService.exportCom(encoded.bytes, 'program.com');
        }),
      ),
    { dispatch: false },
  );

  public readonly importCom$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EmulatorActions.importCom),
      switchMap(() =>
        new Observable<ReturnType<typeof EmulatorActions.updateSourceCode>>(subscriber => {
          this.comFileService.importCom().then(
            ({ data }) => {
              const source = this.decoder.decode(data);
              subscriber.next(EmulatorActions.updateSourceCode({ source }));
              subscriber.complete();
            },
            () => subscriber.complete(),
          );
        }),
      ),
    ),
  );

  public readonly exportAsm$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(EmulatorActions.exportAsm),
        tap(() => {
          const source = this.facade.sourceCode();
          this.comFileService.exportAsm(source, 'program.asm');
        }),
      ),
    { dispatch: false },
  );

  public readonly importAsm$ = createEffect(() =>
    this.actions$.pipe(
      ofType(EmulatorActions.importAsm),
      switchMap(() =>
        new Observable<ReturnType<typeof EmulatorActions.updateSourceCode>>(subscriber => {
          this.comFileService.importAsm().then(
            ({ data }) => {
              subscriber.next(EmulatorActions.updateSourceCode({ source: data }));
              subscriber.complete();
            },
            () => subscriber.complete(),
          );
        }),
      ),
    ),
  );

  private createBatchRun(engine: ExecutionEngine): Observable<EmulatorEffectAction> {
    return new Observable(subscriber => {
      engine.run(() => {
        this.emitBatchResult(engine, subscriber);
        subscriber.complete();
      }, () => {
        this.emitBatchResult(engine, subscriber);
      });

      return () => engine.pause();
    });
  }

  private finishWithBatchRun(
    engine: ExecutionEngine,
    subscriber: Subscriber<EmulatorEffectAction>,
  ): void {
    engine.run(() => {
      this.emitBatchResult(engine, subscriber);
      subscriber.complete();
    }, () => {
      this.emitBatchResult(engine, subscriber);
    });
  }

  private emitBatchResult(
    engine: ExecutionEngine,
    subscriber: Subscriber<EmulatorEffectAction>,
  ): void {
    const status = engine.getStatus();
    if (status === ExecutionStatus.Error) {
      subscriber.next(EmulatorActions.executionError({ error: engine.getError() ?? 'Unknown error' }));
    } else {
      const newOutput = engine.flushConsoleOutput().join('');
      const consoleOutput = [...this.facade.consoleOutput()];
      if (newOutput.length > 0) {
        consoleOutput.push(newOutput);
      }
      if (status === ExecutionStatus.Halted) {
        consoleOutput.push('[Info] Program halted');
      }
      subscriber.next(
        EmulatorActions.stepSuccess({
          cpu: engine.getCpu(),
          memory: Array.from(engine.getMemory().snapshot()),
          status,
          consoleOutput,
        }),
      );
    }
  }

  private createAnimatedRun(engine: ExecutionEngine): Observable<EmulatorEffectAction> {
    return new Observable(subscriber => {
      let cancelled = false;

      const tick = (): void => {
        if (cancelled) return;

        const result = engine.step();

        if (result.status === ExecutionStatus.Error) {
          subscriber.next(EmulatorActions.executionError({ error: engine.getError() ?? 'Unknown error' }));
          subscriber.complete();
          return;
        }

        const halted = result.status === ExecutionStatus.Halted;
        const hitBreakpoint = !halted && engine.getBreakpoints().has(result.cpu.ip);
        const terminal = halted || hitBreakpoint;

        subscriber.next(
          EmulatorActions.stepSuccess({
            cpu: result.cpu,
            memory: Array.from(result.memorySnapshot),
            status: terminal ? (halted ? ExecutionStatus.Halted : ExecutionStatus.Paused) : ExecutionStatus.Running,
            output: halted ? '[Info] Program halted' : result.output,
            trace: result.trace,
          }),
        );

        if (terminal) {
          subscriber.complete();
          return;
        }

        // Re-read delay each tick so mid-run speed changes take effect immediately
        const currentDelay = this.facade.executionDelayMs();
        if (currentDelay <= 0) {
          // Switched to full-speed mid-run — hand off to batch execution
          this.finishWithBatchRun(engine, subscriber);
          return;
        }

        // If a sleep was triggered by INT 86h, use the larger of sleep vs animation delay
        const sleepRemaining = engine.getSleepRemaining();
        const nextDelay = sleepRemaining > 0 ? Math.max(sleepRemaining, currentDelay) : currentDelay;

        setTimeout(tick, nextDelay);
      };

      tick();

      return () => {
        cancelled = true;
        engine.pause();
      };
    });
  }
}
