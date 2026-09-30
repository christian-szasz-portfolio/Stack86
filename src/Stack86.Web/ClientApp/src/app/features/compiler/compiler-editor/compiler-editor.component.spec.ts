import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, it, expect } from 'vitest';
import { provideStore } from '@ngrx/store';
import { CompilerEditorComponent } from './compiler-editor.component';
import { compilerReducer } from '../../../state/compiler.reducer';
import { COMPILER_FEATURE_KEY } from '../../../state/compiler.state';
import { provideTestIcons } from '../../../testing/provide-icons';

describe('CompilerEditorComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompilerEditorComponent],
      providers: [
        provideStore({ [COMPILER_FEATURE_KEY]: compilerReducer }),
        provideTestIcons(),
      ],
    }).compileComponents();
  });

  it('should create', () => {
    const fixture = TestBed.createComponent(CompilerEditorComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('should render the editor container', () => {
    const fixture = TestBed.createComponent(CompilerEditorComponent);
    fixture.detectChanges();
    const container = fixture.nativeElement.querySelector('.editor-container');
    expect(container).toBeTruthy();
  });
});
