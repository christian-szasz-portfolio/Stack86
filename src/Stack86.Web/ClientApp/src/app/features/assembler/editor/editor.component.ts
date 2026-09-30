import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { PanelHeaderComponent } from '@shared/components';
import { EmulatorFacade } from '../../../state/emulator.facade';
import { EditorStorageService, MonacoEditorService } from '../../../core/editor';
import { AssemblerBridgeService } from '../../../core/compiler/assembler-bridge.service';
import { SerializedParseError } from '../../../state/emulator.state';
import type * as Monaco from 'monaco-editor';

@Component({
  host: {
    '(window:keydown)': 'onKeyDown($event)',
  },
  selector: 'emu-editor',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PanelHeaderComponent],
  templateUrl: './editor.component.html',
  styleUrl: './editor.component.scss',
})
export class EditorComponent implements OnDestroy {
  private readonly facade = inject(EmulatorFacade);
  private readonly monacoService = inject(MonacoEditorService);
  private readonly editorStorage = inject(EditorStorageService);
  private readonly assemblerBridge = inject(AssemblerBridgeService);
  private readonly editorContainer = viewChild.required<ElementRef<HTMLElement>>('editorContainer');
  private editor: Monaco.editor.IStandaloneCodeEditor | null = null;
  private monaco: typeof Monaco | null = null;
  private lineDecorations: Monaco.editor.IEditorDecorationsCollection | null = null;
  private breakpointDecorations: Monaco.editor.IEditorDecorationsCollection | null = null;
  private readonly breakpointLines = new Set<number>();
  public readonly ready = signal(false);
  private suppressStoreSync = false;

  public constructor() {
    // After render, so Monaco measures a container the browser has already laid out.
    afterNextRender(() => {
      // initialiseEditor is async and nothing awaits it; without this handler a failed Monaco
      // chunk load surfaces only as an unhandled rejection.
      void this.initialiseEditor().catch((error: unknown) => {
        console.error('Failed to initialise the assembler editor.', error);
      });
    });

    effect(() => {
      const inst = this.facade.currentInstruction();
      this.highlightLine(inst?.line ?? null);
    });

    // Sync store → editor when source changes externally (e.g. Load Example)
    effect(() => {
      const storeSource = this.facade.sourceCode();
      if (this.editor && !this.suppressStoreSync) {
        const editorSource = this.editor.getValue();
        if (storeSource !== editorSource) {
          this.suppressStoreSync = true;
          this.editor.setValue(storeSource);
          this.suppressStoreSync = false;
        }
      }
    });

    // Show/clear parse error markers in the editor
    effect(() => {
      const errors = this.facade.parseErrors();
      this.setErrorMarkers(errors);
    });
  }

  public onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'F5' && !event.shiftKey) {
      event.preventDefault();
      this.facade.run();
    } else if (event.key === 'F5' && event.shiftKey) {
      event.preventDefault();
      this.facade.reset();
    } else if (event.key === 'F10') {
      event.preventDefault();
      this.facade.step();
    } else if (event.key === 'F6') {
      event.preventDefault();
      this.facade.pause();
    } else if (event.key === 'Enter' && event.ctrlKey) {
      event.preventDefault();
      this.facade.loadProgram(this.facade.sourceCode());
    }
  }

  private async initialiseEditor(): Promise<void> {
    const monaco = await this.monacoService.loadMonaco();
    this.monaco = monaco;

    const { ASM_8086_LANGUAGE_ID, asm8086LanguageDef, asm8086ThemeDef } = await import('./monaco-8086-language');

    monaco.languages.register({ id: ASM_8086_LANGUAGE_ID });
    monaco.languages.setMonarchTokensProvider(ASM_8086_LANGUAGE_ID, asm8086LanguageDef);
    monaco.editor.defineTheme('asm-dark', asm8086ThemeDef);

    const pendingAssembly = this.assemblerBridge.consumePendingAssembly();
    const savedSource = await this.editorStorage.loadAssemblerSource();

    this.editor = monaco.editor.create(this.editorContainer().nativeElement, {
      value: pendingAssembly || savedSource || this.facade.sourceCode() || DEFAULT_SOURCE,
      language: ASM_8086_LANGUAGE_ID,
      theme: 'asm-dark',
      fontSize: 14,
      lineNumbers: 'on',
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      automaticLayout: true,
      tabSize: 4,
      renderWhitespace: 'none',
      wordWrap: 'off',
      glyphMargin: true,
    });

    this.lineDecorations = this.editor.createDecorationsCollection([]);
    this.breakpointDecorations = this.editor.createDecorationsCollection([]);

    this.editor.onDidChangeModelContent(() => {
      if (this.suppressStoreSync) return;
      const content = this.editor?.getValue() ?? '';
      this.facade.updateSourceCode(content);
    });

    // Sync the initial editor content to the store (Monaco's create()
    // does not fire onDidChangeModelContent for the initial value).
    this.facade.updateSourceCode(this.editor.getValue());

    if (pendingAssembly) {
      this.facade.loadProgram(pendingAssembly);
    }

    this.editor.onMouseDown((e) => {
      if (e.target.type === monaco.editor.MouseTargetType.GUTTER_GLYPH_MARGIN) {
        const line = e.target.position?.lineNumber;
        if (line) this.toggleBreakpoint(line);
      }
    });

    this.ready.set(true);
  }

  public ngOnDestroy(): void {
    if (this.editor) {
      this.monacoService.dispose(this.editor);
    }
  }

  private toggleBreakpoint(monacoLine: number): void {
    const address = monacoLine - 1; // 0-based instruction address approximation
    if (this.breakpointLines.has(monacoLine)) {
      this.breakpointLines.delete(monacoLine);
      this.facade.removeBreakpoint(address);
    } else {
      this.breakpointLines.add(monacoLine);
      this.facade.setBreakpoint(address);
    }
    this.updateBreakpointDecorations();
  }

  private updateBreakpointDecorations(): void {
    if (!this.breakpointDecorations || !this.monaco) return;
    const decorations: Monaco.editor.IModelDeltaDecoration[] = [];
    for (const line of this.breakpointLines) {
      decorations.push({
        range: new this.monaco.Range(line, 1, line, 1),
        options: {
          isWholeLine: false,
          glyphMarginClassName: 'breakpoint-glyph',
        },
      });
    }
    this.breakpointDecorations.set(decorations);
  }

  private highlightLine(line: number | null): void {
    if (!this.lineDecorations || !this.monaco) return;
    if (line === null || line < 0) {
      this.lineDecorations.set([]);
      return;
    }
    const monacoLine = line + 1;
    this.lineDecorations.set([
      {
        range: new this.monaco.Range(monacoLine, 1, monacoLine, 1),
        options: {
          isWholeLine: true,
          className: 'current-line-highlight',
          glyphMarginClassName: 'current-line-glyph',
        },
      },
    ]);
    this.editor?.revealLineInCenterIfOutsideViewport(monacoLine);
  }

  private setErrorMarkers(errors: SerializedParseError[]): void {
    if (!this.monaco || !this.editor) return;
    const model = this.editor.getModel();
    if (!model) return;

    if (errors.length === 0) {
      this.monaco.editor.setModelMarkers(model, 'asm-parser', []);
      return;
    }

    const markers: Monaco.editor.IMarkerData[] = errors.map((err) => {
      const monacoLine = err.line + 1;
      const monacoCol = err.column + 1;
      const lineLength = model.getLineLength(monacoLine);
      return {
        severity: err.severity === 'warning'
          ? this.monaco!.MarkerSeverity.Warning
          : this.monaco!.MarkerSeverity.Error,
        message: err.message,
        startLineNumber: monacoLine,
        startColumn: monacoCol,
        endLineNumber: monacoLine,
        endColumn: lineLength + 1,
      };
    });

    this.monaco.editor.setModelMarkers(model, 'asm-parser', markers);
  }
}

const DEFAULT_SOURCE = `; Stack86 — 8086 Assembly
; Prints "HELLO" and computes factorial of 5

; --- Print "HELLO" using INT 21h (AH=02h) ---
MOV AX, 0x0200
MOV DX, 72        ; 'H'
INT 0x21
MOV DX, 69        ; 'E'
INT 0x21
MOV DX, 76        ; 'L'
INT 0x21
MOV DX, 76        ; 'L'
INT 0x21
MOV DX, 79        ; 'O'
INT 0x21

; --- Factorial of 5 (5! = 120) ---
MOV CX, 5         ; n = 5
MOV AX, 1         ; accumulator = 1

fact_loop:
  MUL CX          ; AX = AX * CX
  DEC CX
  JNZ fact_loop

; AX = 120 (0x0078)
MOV BX, AX        ; save result in BX

; --- Push/pop to show the stack ---
PUSH BX
PUSH CX
POP DX             ; DX = 0 (CX was 0)
POP AX             ; AX = 120 (our factorial)

HLT
`;
