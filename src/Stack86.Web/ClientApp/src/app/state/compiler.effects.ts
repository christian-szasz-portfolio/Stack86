import { Service, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { of } from 'rxjs';
import { catchError, debounceTime, map, switchMap, tap, withLatestFrom } from 'rxjs/operators';
import { CompilerActions } from './compiler.actions';
import { selectSelectedLanguage, selectAllFilesAsDict, selectAssembly, selectFiles } from './compiler.selectors';
import { CompilerService } from '../core/compiler/compiler.service';
import { AssemblerBridgeService } from '../core/compiler/assembler-bridge.service';
import { CompilerStorageUtility } from '../core/compiler/compiler-storage.utility';
import { getSamplesForLanguage } from '../features/compiler/compile-toolbar/sample-programs';
import { EditorStorageService } from '../core/editor';
import { CircuitOpenError } from '../core/resilience';

@Service({ autoProvided: false })
export class CompilerEffects {
  private readonly actions$ = inject(Actions);
  private readonly store = inject(Store);
  private readonly compilerService = inject(CompilerService);
  private readonly assemblerBridge = inject(AssemblerBridgeService);
  private readonly router = inject(Router);
  private readonly editorStorage = inject(EditorStorageService);

  public readonly compile$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CompilerActions.compile),
      withLatestFrom(
        this.store.select(selectSelectedLanguage),
        this.store.select(selectAllFilesAsDict),
      ),
      switchMap(([, language, files]) =>
        this.compilerService.compileStream(language, files).pipe(
          map((event) => {
            if (event.type === 'log') {
              return CompilerActions.compileLog({ text: event.text });
            }
            if (event.type === 'result') {
              return CompilerActions.compileSuccess({
                assembly: event.assembly ?? null,
                errors: event.errors,
                warnings: event.warnings,
              });
            }
            // heartbeat events are filtered upstream; treat as a no-op log.
            return CompilerActions.compileLog({ text: '' });
          }),
          catchError((error: unknown) => {
            if (error instanceof CircuitOpenError) {
              return of(CompilerActions.compileBlocked({ remainingCooldownMs: error.remainingCooldownMs }));
            }
            const message = error instanceof Error ? error.message : 'Compilation request failed';
            return of(CompilerActions.compileFailure({ error: message }));
          }),
        ),
      ),
    ),
  );

  public readonly persistFiles$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(CompilerActions.updateFileContent, CompilerActions.addFile, CompilerActions.removeFile, CompilerActions.renameFile, CompilerActions.loadProject, CompilerActions.selectLanguage),
        debounceTime(1000),
        withLatestFrom(this.store.select(selectFiles)),
        tap(([, files]) => {
          this.editorStorage.saveCompilerProject(files);
        }),
      ),
    { dispatch: false },
  );

  public readonly loadAssemblyIntoAssembler$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(CompilerActions.loadAssemblyIntoAssembler),
        withLatestFrom(
          this.store.select(selectAssembly),
          this.store.select(selectSelectedLanguage),
        ),
        tap(([, assembly, language]) => {
          if (assembly) {
            const sampleIndex = CompilerStorageUtility.loadSample(language);
            let sampleName: string | null = null;
            if (sampleIndex !== null) {
              const samples = getSamplesForLanguage(language);
              if (sampleIndex >= 0 && sampleIndex < samples.length) {
                sampleName = samples[sampleIndex].name;
              }
            }
            this.assemblerBridge.loadAssemblyIntoAssembler(assembly, { language, sampleName });
            this.router.navigate(['/assembler']);
          }
        }),
      ),
    { dispatch: false },
  );
}
