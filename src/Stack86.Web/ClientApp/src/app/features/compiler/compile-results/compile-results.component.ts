import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  effect,
  inject,
  viewChild,
} from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { EmptyStateComponent, PanelHeaderComponent } from '@shared/components';
import { CompilerFacade } from '../../../state/compiler.facade';
import { MonacoEditorService } from '../../../core/editor';
import type * as Monaco from 'monaco-editor';

@Component({
  selector: 'emu-compile-results',
  imports: [PanelHeaderComponent, FaIconComponent, EmptyStateComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compile-results.component.html',
  styleUrl: './compile-results.component.scss',
})
export class CompileResultsComponent implements OnDestroy {
  private readonly facade = inject(CompilerFacade);
  private readonly monacoService = inject(MonacoEditorService);
  private readonly editorContainer = viewChild.required<ElementRef<HTMLElement>>('editorContainer');

  public readonly loading = this.facade.loading;
  public readonly assembly = this.facade.assembly;
  public readonly showEmptyState = computed(() => !this.assembly() && !this.loading());

  private editor: Monaco.editor.IStandaloneCodeEditor | null = null;

  public constructor() {
    afterNextRender(() => {
      // initMonaco is async and nothing awaits it; without this handler a failed Monaco chunk
      // load surfaces only as an unhandled rejection.
      void this.initMonaco().catch((error: unknown) => {
        console.error('Failed to initialise the generated-assembly viewer.', error);
      });
    });

    effect(() => {
      const asm = this.assembly();
      if (this.editor) {
        this.editor.setValue(asm ?? '');
        if (asm) {
          requestAnimationFrame(() => this.editor?.layout());
        }
      }
    });
  }

  public ngOnDestroy(): void {
    if (this.editor) {
      this.monacoService.dispose(this.editor);
    }
  }

  private async initMonaco(): Promise<void> {
    const monaco = await this.monacoService.loadMonaco();

    const { ASM_8086_LANGUAGE_ID, asm8086LanguageDef, asm8086ThemeDef } = await import(
      '../../assembler/editor/monaco-8086-language'
    );

    if (!monaco.languages.getLanguages().some((l) => l.id === ASM_8086_LANGUAGE_ID)) {
      monaco.languages.register({ id: ASM_8086_LANGUAGE_ID });
      monaco.languages.setMonarchTokensProvider(ASM_8086_LANGUAGE_ID, asm8086LanguageDef);
    }

    if (!document.querySelector('[data-theme-asm-dark]')) {
      monaco.editor.defineTheme('asm-dark', asm8086ThemeDef);
      const marker = document.createElement('meta');
      marker.setAttribute('data-theme-asm-dark', '');
      document.head.appendChild(marker);
    }

    this.editor = monaco.editor.create(this.editorContainer().nativeElement, {
      value: this.facade.assembly() ?? '',
      language: ASM_8086_LANGUAGE_ID,
      theme: 'asm-dark',
      readOnly: true,
      fontSize: 13,
      fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
      minimap: { enabled: false },
      lineNumbers: 'on',
      scrollBeyondLastLine: false,
      automaticLayout: true,
      renderLineHighlight: 'none',
      domReadOnly: true,
      contextmenu: false,
    });
  }
}
