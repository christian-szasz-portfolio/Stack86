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
import { MenuDefinition, MenuItem } from './menu-definitions';

@Component({
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
  selector: 'emu-menu-bar',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './menu-bar.component.html',
  styleUrl: './menu-bar.component.scss',
})
export class MenuBarComponent {
  public readonly menus = input.required<MenuDefinition[]>();
  public readonly menuAction = output<string>();

  protected readonly openMenuIndex = signal<number | null>(null);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);

  public onDocumentClick(event: MouseEvent): void {
    if (!this.elementRef.nativeElement.contains(event.target as Node)) {
      this.openMenuIndex.set(null);
    }
  }

  public onEscape(): void {
    this.openMenuIndex.set(null);
  }

  protected toggleMenu(index: number): void {
    this.openMenuIndex.set(this.openMenuIndex() === index ? null : index);
  }

  protected onMenuHover(index: number): void {
    if (this.openMenuIndex() !== null) {
      this.openMenuIndex.set(index);
    }
  }

  protected onItemClick(item: MenuItem): void {
    if (item.disabled || item.separator || !item.action) {
      return;
    }
    this.menuAction.emit(item.action);
    this.openMenuIndex.set(null);
  }
}
