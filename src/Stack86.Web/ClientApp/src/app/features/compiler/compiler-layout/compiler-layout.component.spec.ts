import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, it, expect } from 'vitest';
import { provideStore } from '@ngrx/store';
import { CompilerLayoutComponent } from './compiler-layout.component';
import { compilerReducer } from '../../../state/compiler.reducer';
import { COMPILER_FEATURE_KEY } from '../../../state/compiler.state';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('CompilerLayoutComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompilerLayoutComponent],
      providers: [provideStore({ [COMPILER_FEATURE_KEY]: compilerReducer }), provideTestIcons()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the compile toolbar', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const toolbar = fixture.nativeElement.querySelector('emu-compile-toolbar');
    expect(toolbar).toBeTruthy();
  });

  it('should render the compiler editor', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const editor = fixture.nativeElement.querySelector('emu-compiler-editor');
    expect(editor).toBeTruthy();
  });

  it('should render the compile results', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const results = fixture.nativeElement.querySelector('emu-compile-results');
    expect(results).toBeTruthy();
  });

  it('should render the compile console', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const console = fixture.nativeElement.querySelector('emu-compile-console');
    expect(console).toBeTruthy();
  });

  it('should render the angular-split container', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const split = fixture.nativeElement.querySelector('as-split');
    expect(split).toBeTruthy();
  });

  it('should have nested splits with four split areas', () => {
    const fixture = TestBed.createComponent(CompilerLayoutComponent);
    fixture.detectChanges();
    const areas = fixture.nativeElement.querySelectorAll('as-split-area');
    expect(areas.length).toBe(4);
  });
});
