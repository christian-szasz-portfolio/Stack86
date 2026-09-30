import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, it, expect } from 'vitest';
import { provideStore, Store } from '@ngrx/store';
import { CompileConsoleComponent } from './compile-console.component';
import { CompilerActions } from '../../../state/compiler.actions';
import { compilerReducer } from '../../../state/compiler.reducer';
import { COMPILER_FEATURE_KEY } from '../../../state/compiler.state';
import { provideTestIcons } from '../../../testing/provide-icons';
import { ConsoleTab } from '@shared/components';

describe('CompileConsoleComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompileConsoleComponent],
      providers: [provideStore({ [COMPILER_FEATURE_KEY]: compilerReducer }), provideTestIcons()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CompileConsoleComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the tab bar', () => {
    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.detectChanges();
    const tabBar = fixture.nativeElement.querySelector('emu-console-tab-bar');
    expect(tabBar).toBeTruthy();
  });

  it('should show empty state on build log tab by default', () => {
    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.detectChanges();
    const empty = fixture.nativeElement.querySelector('emu-empty-state');
    expect(empty).toBeTruthy();
  });

  it('should show loading spinner on build log tab when compiling', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.BuildLog);
    fixture.detectChanges();

    const status = fixture.nativeElement.querySelector('.console-status');
    expect(status).toBeTruthy();
  });

  it('should show compilation error on problems tab', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(CompilerActions.compileFailure({ error: 'Server error' }));

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    const lines = fixture.nativeElement.querySelectorAll('.console-line--error');
    expect(lines.length).toBeGreaterThan(0);
    expect(lines[0].textContent).toContain('Server error');
  });

  it('should show diagnostic errors on problems tab', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(
      CompilerActions.compileSuccess({
        assembly: null,
        errors: [{ message: 'undeclared variable', line: 3, column: 5 }],
        warnings: [],
      }),
    );

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    const errorLine = fixture.nativeElement.querySelector('.console-line--error');
    expect(errorLine).toBeTruthy();
    expect(errorLine.textContent).toContain('undeclared variable');
  });

  it('should show streamed console messages on build log tab', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(CompilerActions.compileLog({ text: '[Info] Compiling 5 lines of C...' }));
    store.dispatch(CompilerActions.compileLog({ text: '[Info] TCC validation passed' }));

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.BuildLog);
    fixture.detectChanges();

    const infoLines = fixture.nativeElement.querySelectorAll('.console-line--info');
    expect(infoLines.length).toBe(2);
    expect(infoLines[0].textContent).toContain('Compiling 5 lines');
    expect(infoLines[1].textContent).toContain('TCC validation passed');

    const status = fixture.nativeElement.querySelector('.console-status');
    expect(status).toBeTruthy();
  });

  it('should categorize [Error] and [Warn] console messages on build log tab', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(CompilerActions.compileLog({ text: '[Info] Compiling...' }));
    store.dispatch(CompilerActions.compileLog({ text: '[Warn] javac not found. External validation skipped.' }));
    store.dispatch(CompilerActions.compileLog({ text: '[Error] External validation failed' }));

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.BuildLog);
    fixture.detectChanges();

    const infoLines = fixture.nativeElement.querySelectorAll('.console-line--info');
    const warnLines = fixture.nativeElement.querySelectorAll('.console-line--warning');
    const errorLines = fixture.nativeElement.querySelectorAll('.console-line--error');
    expect(infoLines.length).toBe(1);
    expect(warnLines.length).toBe(1);
    expect(errorLines.length).toBe(1);
    expect(warnLines[0].textContent).toContain('javac not found');
    expect(errorLines[0].textContent).toContain('External validation failed');
  });

  it('should include the source file in problem diagnostic locations', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(
      CompilerActions.compileSuccess({
        assembly: null,
        errors: [{ message: 'undeclared variable', line: 3, column: 5, file: 'helper.c' }],
        warnings: [],
      }),
    );

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    const errorLine = fixture.nativeElement.querySelector('.console-line--error');
    expect(errorLine).toBeTruthy();
    expect(errorLine.textContent).toContain('helper.c:3:5');
  });

  it('should show diagnostic warnings on problems tab', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(
      CompilerActions.compileSuccess({
        assembly: 'MOV AX, 1',
        errors: [],
        warnings: [{ message: 'unused variable', line: 2, column: 1 }],
      }),
    );

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    const warningLine = fixture.nativeElement.querySelector('.console-line--warning');
    expect(warningLine).toBeTruthy();
    expect(warningLine.textContent).toContain('unused variable');
  });

  it('should show no-problems empty state when compilation succeeds', () => {
    const store = TestBed.inject(Store);
    store.dispatch(CompilerActions.compile());
    store.dispatch(
      CompilerActions.compileSuccess({
        assembly: 'MOV AX, 1',
        errors: [],
        warnings: [],
      }),
    );

    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    const empty = fixture.nativeElement.querySelector('emu-empty-state');
    expect(empty).toBeTruthy();
  });

  it('should auto-switch to build log when compilation starts', () => {
    const store = TestBed.inject(Store);
    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.componentInstance.activeTab.set(ConsoleTab.Problems);
    fixture.detectChanges();

    store.dispatch(CompilerActions.compile());
    fixture.detectChanges();

    expect(fixture.componentInstance.activeTab()).toBe(ConsoleTab.BuildLog);
  });

  it('should auto-switch to problems when errors arrive', () => {
    const store = TestBed.inject(Store);
    const fixture = TestBed.createComponent(CompileConsoleComponent);
    fixture.detectChanges();

    store.dispatch(CompilerActions.compile());
    fixture.detectChanges();

    store.dispatch(
      CompilerActions.compileSuccess({
        assembly: null,
        errors: [{ message: 'syntax error', line: 1, column: 1 }],
        warnings: [],
      }),
    );
    fixture.detectChanges();

    expect(fixture.componentInstance.activeTab()).toBe(ConsoleTab.Problems);
  });
});
