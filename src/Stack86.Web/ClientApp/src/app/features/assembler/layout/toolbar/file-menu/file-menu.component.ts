import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

export interface FileMenuItem {
  readonly label: string;
  readonly icon?: string;
  readonly action: string;
  readonly separator?: boolean;
  readonly disabled?: boolean;
}

@Component({
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
  selector: 'emu-file-menu',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './file-menu.component.html',
  styleUrl: './file-menu.component.scss',
})
export class FileMenuComponent {
  private readonly elementRef = inject(ElementRef);

  /** Whether export COM should be disabled (e.g. no assembled program). */
  public readonly exportDisabled = input(false);

  public readonly menuAction = output<string>();

  protected readonly isOpen = signal(false);

  protected readonly items: FileMenuItem[] = [
    { label: 'Import ASM...', icon: 'folder-open', action: 'importAsm' },
    { label: 'Export ASM', icon: 'floppy-disk', action: 'exportAsm' },
    { label: '', action: '', separator: true },
    { label: 'Import COM...', icon: 'upload', action: 'importCom' },
    { label: 'Export COM', icon: 'file', action: 'exportCom' },
  ];

  protected toggle(): void {
    this.isOpen.update((v) => !v);
  }

  protected onItemClick(item: FileMenuItem): void {
    if (item.separator || item.disabled) {
      return;
    }
    if (item.action === 'exportCom' && this.exportDisabled()) {
      return;
    }
    this.menuAction.emit(item.action);
    this.isOpen.set(false);
  }

  protected isItemDisabled(item: FileMenuItem): boolean {
    if (item.action === 'exportCom') {
      return this.exportDisabled();
    }
    return item.disabled ?? false;
  }

  public onDocumentClick(event: MouseEvent): void {
    if (this.isOpen() && !this.elementRef.nativeElement.contains(event.target as Node)) {
      this.isOpen.set(false);
    }
  }

  public onEscape(): void {
    this.isOpen.set(false);
  }
}
