import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { CompilerFile } from '../../../../core/compiler/compiler.models';

@Component({
  selector: 'emu-tab-bar',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './tab-bar.component.html',
  styleUrl: './tab-bar.component.scss',
})
export class TabBarComponent {
  public readonly files = input.required<CompilerFile[]>();
  public readonly activeFileId = input.required<string>();

  public readonly tabSelected = output<string>();
  public readonly tabClosed = output<string>();
  public readonly addFileRequested = output<void>();

  public onTabClick(fileId: string): void {
    this.tabSelected.emit(fileId);
  }

  public onCloseClick(event: Event, fileId: string): void {
    event.stopPropagation();
    this.tabClosed.emit(fileId);
  }

  public onAddClick(): void {
    this.addFileRequested.emit();
  }

  public getFileIcon(name: string): string {
    return name.endsWith('.h') ? 'file-lines' : 'file-code';
  }
}
