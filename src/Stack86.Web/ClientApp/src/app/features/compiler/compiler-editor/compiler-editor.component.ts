import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  OnDestroy,
  afterNextRender,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { CompilerFacade } from '../../../state/compiler.facade';
import { EditorStorageService, MonacoEditorService } from '../../../core/editor';
import {
  CompilerFile,
  LANGUAGE_OPTIONS,
  getAllExtensions,
  isHeaderExtension,
} from '../../../core/compiler/compiler.models';
import { MenuBarComponent } from './menu-bar/menu-bar.component';
import { TabBarComponent } from './tab-bar/tab-bar.component';
import { EDIT_MENU, NEW_FILE_ACTION_PREFIX, buildFileMenu } from './menu-bar/menu-definitions';

@Component({
  selector: 'emu-compiler-editor',
  imports: [MenuBarComponent, TabBarComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './compiler-editor.component.html',
  styleUrl: './compiler-editor.component.scss',
})
export class CompilerEditorComponent implements OnDestroy {
  private readonly facade = inject(CompilerFacade);
  private readonly monacoService = inject(MonacoEditorService);
  private readonly editorStorage = inject(EditorStorageService);
  private readonly editorContainer = viewChild.required<ElementRef<HTMLElement>>('editorContainer');
  private readonly fileInput = viewChild.required<ElementRef<HTMLInputElement>>('fileInput');
  private readonly selectedLanguage = this.facade.selectedLanguage;

  protected readonly files = this.facade.files;
  protected readonly activeFileId = this.facade.activeFileId;
  protected readonly showNewFileInput = signal(false);
  protected readonly newFileName = signal('');
  protected readonly menus = computed(() => [buildFileMenu(this.selectedLanguage()), EDIT_MENU]);
  protected readonly fileInputAccept = computed(() => getAllExtensions(this.selectedLanguage()).join(','));

  private editor: import('monaco-editor').editor.IStandaloneCodeEditor | null = null;
  private monaco: typeof import('monaco-editor') | null = null;
  private readonly models = new Map<string, import('monaco-editor').editor.ITextModel>();
  private isUpdatingFromStore = false;
  private isConfirmingNewFile = false;
  private newFileExtension = '.c';

  constructor() {
    afterNextRender(() => {
      // initMonaco is async and nothing awaits it; without this handler a failed Monaco chunk
      // load surfaces only as an unhandled rejection.
      void this.initMonaco().catch((error: unknown) => {
        console.error('Failed to initialise the source editor.', error);
      });
    });

    effect(() => {
      const lang = this.selectedLanguage();
      if (this.monaco) {
        const langOption = LANGUAGE_OPTIONS.find((l) => l.id === lang);
        if (langOption) {
          for (const model of this.models.values()) {
            this.monaco.editor.setModelLanguage(model, langOption.monacoLanguage);
          }
        }
      }
    });

    effect(() => {
      const files = this.files();
      const activeId = this.activeFileId();
      if (!this.editor || !this.monaco) {
        return;
      }
      this.syncModels(files);
      this.switchToModel(activeId);
    });
  }

  public ngOnDestroy(): void {
    for (const model of this.models.values()) {
      model.dispose();
    }
    this.models.clear();
    if (this.editor) {
      this.monacoService.dispose(this.editor);
    }
  }

  protected onTabSelected(fileId: string): void {
    this.facade.setActiveFile(fileId);
  }

  protected onTabClosed(fileId: string): void {
    const model = this.models.get(fileId);
    if (model) {
      model.dispose();
      this.models.delete(fileId);
    }
    this.facade.removeFile(fileId);
  }

  protected onAddFileRequested(): void {
    const ext = LANGUAGE_OPTIONS.find((l) => l.id === this.selectedLanguage())?.sourceExtensions[0] ?? '.c';
    this.newFileExtension = ext;
    this.showNewFileInput.set(true);
    this.newFileName.set(this.generateFileName(ext));
    requestAnimationFrame(() => {
      const input = document.querySelector<HTMLInputElement>('.new-file-input__field');
      input?.focus();
      input?.select();
    });
  }

  protected confirmNewFile(name: string): void {
    if (this.isConfirmingNewFile) {
      return;
    }
    this.isConfirmingNewFile = true;

    const trimmed = name.trim();
    const allowed = getAllExtensions(this.selectedLanguage());
    if (!trimmed || !allowed.some((e) => trimmed.endsWith(e))) {
      this.cancelNewFile();
      this.isConfirmingNewFile = false;
      return;
    }
    const existing = this.files().find((f) => f.name === trimmed);
    if (existing) {
      this.cancelNewFile();
      this.isConfirmingNewFile = false;
      return;
    }
    const file: CompilerFile = {
      id: crypto.randomUUID(),
      name: trimmed,
      content: '',
      isMain: false,
    };
    this.facade.addFile(file);
    this.showNewFileInput.set(false);
    this.isConfirmingNewFile = false;
  }

  protected cancelNewFile(): void {
    this.showNewFileInput.set(false);
  }

  protected onMenuAction(action: string): void {
    if (action.startsWith(NEW_FILE_ACTION_PREFIX)) {
      const ext = action.substring(NEW_FILE_ACTION_PREFIX.length);
      this.newFileExtension = ext;
      this.showNewFileInput.set(true);
      this.newFileName.set(this.generateFileName(ext));
      requestAnimationFrame(() => {
        const input = document.querySelector<HTMLInputElement>('.new-file-input__field');
        input?.focus();
        input?.select();
      });
      return;
    }
    switch (action) {
      case 'openFile':
        this.fileInput().nativeElement.click();
        break;
      case 'save':
        this.saveActiveFile();
        break;
      case 'saveAll':
        this.saveAllFiles();
        break;
      case 'closeFile':
        this.closeActiveFile();
        break;
      case 'undo':
        this.editor?.trigger('menu', 'undo', null);
        break;
      case 'redo':
        this.editor?.trigger('menu', 'redo', null);
        break;
      case 'cut':
        this.editor?.trigger('menu', 'editor.action.clipboardCutAction', null);
        break;
      case 'copy':
        this.editor?.trigger('menu', 'editor.action.clipboardCopyAction', null);
        break;
      case 'paste':
        this.editor?.focus();
        navigator.clipboard.readText().then((text) => {
          this.editor?.trigger('menu', 'type', { text });
        });
        break;
      case 'selectAll':
        this.editor?.trigger('menu', 'editor.action.selectAll', null);
        break;
    }
  }

  protected onFilesSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const fileList = input.files;
    if (!fileList) {
      return;
    }
    const allowed = getAllExtensions(this.selectedLanguage());
    for (let i = 0; i < fileList.length; i++) {
      const file = fileList[i];
      if (!allowed.some((e) => file.name.endsWith(e))) {
        continue;
      }
      file.text().then((content) => {
        const existing = this.files().find((f) => f.name === file.name);
        if (existing) {
          this.facade.updateFileContent(existing.id, content);
        } else {
          const newFile: CompilerFile = {
            id: crypto.randomUUID(),
            name: file.name,
            content,
            isMain: false,
          };
          this.facade.addFile(newFile);
        }
      });
    }
    input.value = '';
  }

  private async initMonaco(): Promise<void> {
    this.monaco = await this.monacoService.loadMonaco();

    if (!this.editorStorage.isCompilerHydrated) {
      this.editorStorage.markCompilerHydrated();
      const savedProject = await this.editorStorage.loadCompilerProject();
      if (savedProject && savedProject.length > 0) {
        this.facade.loadProject(savedProject);
      }
    }

    const files = this.files();

    this.editor = this.monaco.editor.create(this.editorContainer().nativeElement, {
      language: 'c',
      theme: 'vs-dark',
      fontSize: 13,
      fontFamily: "'Cascadia Code', 'Fira Code', 'Consolas', monospace",
      minimap: { enabled: false },
      lineNumbers: 'on',
      scrollBeyondLastLine: false,
      automaticLayout: true,
      tabSize: 4,
      insertSpaces: true,
    });

    this.syncModels(files);
    const mainFile = files.find((f) => f.isMain) ?? files[0];
    this.switchToModel(mainFile.id);
  }

  private syncModels(files: CompilerFile[]): void {
    if (!this.monaco) {
      return;
    }
    const currentIds = new Set(files.map((f) => f.id));

    // Remove models for deleted files
    for (const [id, model] of this.models) {
      if (!currentIds.has(id)) {
        model.dispose();
        this.models.delete(id);
      }
    }

    // Create models for new files
    const langOption = LANGUAGE_OPTIONS.find((l) => l.id === this.selectedLanguage());
    const monacoLang = langOption?.monacoLanguage ?? 'c';

    for (const file of files) {
      if (!this.models.has(file.id)) {
        const model = this.monaco.editor.createModel(file.content, monacoLang);
        model.onDidChangeContent(() => {
          if (!this.isUpdatingFromStore) {
            this.facade.updateFileContent(file.id, model.getValue());
          }
        });
        this.models.set(file.id, model);
      }
    }
  }

  private switchToModel(fileId: string): void {
    const model = this.models.get(fileId);
    if (this.editor && model && this.editor.getModel() !== model) {
      this.isUpdatingFromStore = true;
      this.editor.setModel(model);
      this.isUpdatingFromStore = false;
    }
  }

  private saveActiveFile(): void {
    const activeFile = this.files().find((f) => f.id === this.activeFileId());
    if (!activeFile) {
      return;
    }
    this.downloadFile(activeFile.name, activeFile.content);
  }

  private saveAllFiles(): void {
    for (const file of this.files()) {
      this.downloadFile(file.name, file.content);
    }
  }

  private closeActiveFile(): void {
    const activeId = this.activeFileId();
    const activeFile = this.files().find((f) => f.id === activeId);
    if (activeFile && !activeFile.isMain && this.files().length > 1) {
      this.onTabClosed(activeId);
    }
  }

  private downloadFile(name: string, content: string): void {
    const blob = new Blob([content], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = name;
    a.click();
    URL.revokeObjectURL(url);
  }

  private generateFileName(ext: string): string {
    const names = new Set(this.files().map((f) => f.name));
    const base = isHeaderExtension(this.selectedLanguage(), ext) ? 'header' : 'file';
    for (let i = 1; ; i++) {
      const name = `${base}${i}${ext}`;
      if (!names.has(name)) {
        return name;
      }
    }
  }
}
