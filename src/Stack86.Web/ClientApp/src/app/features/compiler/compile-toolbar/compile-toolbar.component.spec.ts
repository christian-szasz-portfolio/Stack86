import { TestBed } from '@angular/core/testing';
import { beforeEach, describe, it, expect, vi } from 'vitest';
import { provideRouter } from '@angular/router';
import { Store } from '@ngrx/store';
import { CompileToolbarComponent } from './compile-toolbar.component';
import { CompilerActions } from '../../../state/compiler.actions';
import { LANGUAGE_OPTIONS, SupportedLanguage } from '../../../core/compiler/compiler.models';
import { provideTestIcons } from '../../../testing/provide-icons';
import { provideTestStores } from '../../../testing/test-helpers';
import { C_SAMPLE_PROGRAMS } from './sample-programs';

describe('CompileToolbarComponent', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CompileToolbarComponent],
      providers: [provideRouter([]), provideTestStores(), provideTestIcons()],
    }).compileComponents();
  });

  it('creates', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });

  it('exposes languageItems built from LANGUAGE_OPTIONS', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    expect(fixture.componentInstance.languageItems.length).toBe(LANGUAGE_OPTIONS.length);
  });

  it('offers every language it lists', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    expect(fixture.componentInstance.languageItems.filter((i) => i.disabled)).toHaveLength(0);
  });

  it('exposes sampleItems for the default language', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    expect(fixture.componentInstance.sampleItems()).toHaveLength(C_SAMPLE_PROGRAMS.length);
  });

  it('renders Language and Load Example dropdowns', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    fixture.detectChanges();
    const dropdowns = fixture.nativeElement.querySelectorAll('emu-dropdown-menu');
    expect(dropdowns.length).toBe(2);
  });

  it('renders the compile button', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    fixture.detectChanges();
    const btn = fixture.nativeElement.querySelector('emu-button[variant="primary"] button');
    expect(btn).toBeTruthy();
    expect(btn.textContent.trim()).toContain('Compile');
  });

  it('dispatches loadProject when loading a sample by index', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    const store = TestBed.inject(Store);
    const dispatchSpy = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);

    fixture.componentInstance.loadSample('0');

    expect(dispatchSpy).toHaveBeenCalledWith(
      expect.objectContaining({
        type: CompilerActions.loadProject.type,
        files: [
          expect.objectContaining({
            name: 'main.c',
            content: C_SAMPLE_PROGRAMS[0].files[0].content,
            isMain: true,
          }),
        ],
      }),
    );
  });

  it('dispatches compile when compile() is called', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    const store = TestBed.inject(Store);
    const dispatchSpy = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);

    fixture.componentInstance.compile();

    expect(dispatchSpy).toHaveBeenCalledWith(CompilerActions.compile());
  });

  it('dispatches selectLanguage on language change', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    const store = TestBed.inject(Store);
    const dispatchSpy = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);

    fixture.componentInstance.onLanguageChange(SupportedLanguage.Cpp);

    expect(dispatchSpy).toHaveBeenCalledWith(
      CompilerActions.selectLanguage({ language: SupportedLanguage.Cpp }),
    );
  });

  it('dispatches loadAssemblyIntoAssembler on loadIntoAssembler()', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    const store = TestBed.inject(Store);
    const dispatchSpy = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);

    fixture.componentInstance.loadIntoAssembler();

    expect(dispatchSpy).toHaveBeenCalledWith(CompilerActions.loadAssemblyIntoAssembler());
  });

  it('ignores out-of-range sample indices without dispatching loadProject', () => {
    const fixture = TestBed.createComponent(CompileToolbarComponent);
    const store = TestBed.inject(Store);
    const dispatchSpy = vi.spyOn(store, 'dispatch').mockImplementation(() => undefined);

    fixture.componentInstance.loadSample('999');

    const loadProjectCalls = dispatchSpy.mock.calls.filter(
      (call) => (call[0] as { type: string }).type === CompilerActions.loadProject.type,
    );
    expect(loadProjectCalls).toHaveLength(0);
  });
});
