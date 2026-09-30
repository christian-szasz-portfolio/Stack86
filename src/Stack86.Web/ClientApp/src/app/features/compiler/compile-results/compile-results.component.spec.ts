import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, it, expect } from 'vitest';
import { provideStore } from '@ngrx/store';
import { CompileResultsComponent } from './compile-results.component';
import { compilerReducer } from '../../../state/compiler.reducer';
import { COMPILER_FEATURE_KEY } from '../../../state/compiler.state';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('CompileResultsComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompileResultsComponent],
      providers: [provideStore({ [COMPILER_FEATURE_KEY]: compilerReducer }), provideTestIcons()],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CompileResultsComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the panel header', () => {
    const fixture = TestBed.createComponent(CompileResultsComponent);
    fixture.detectChanges();
    const header = fixture.nativeElement.querySelector('emu-panel-header');
    expect(header).toBeTruthy();
  });

  it('should render the editor container', () => {
    const fixture = TestBed.createComponent(CompileResultsComponent);
    fixture.detectChanges();
    const container = fixture.nativeElement.querySelector('.editor-container');
    expect(container).toBeTruthy();
  });
});
