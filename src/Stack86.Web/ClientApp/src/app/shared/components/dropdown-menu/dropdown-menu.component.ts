import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { IconProp } from '@fortawesome/fontawesome-svg-core';
import { DropdownMenuItem } from './dropdown-menu.models';

@Component({
  host: {
    '(document:click)': 'onDocumentClick($event)',
    '(document:keydown.escape)': 'onEscape()',
  },
  selector: 'emu-dropdown-menu',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dropdown-menu.component.html',
  styleUrl: './dropdown-menu.component.scss',
})
export class DropdownMenuComponent {
  private readonly elementRef = inject(ElementRef);

  public readonly label = input.required<string>();
  public readonly icon = input<IconProp>();
  public readonly items = input.required<DropdownMenuItem[]>();
  public readonly value = input('');

  public readonly itemSelected = output<string>();

  protected readonly isOpen = signal(false);

  protected readonly displayLabel = computed(() => {
    const val = this.value();
    if (val) {
      const match = this.items().find((i) => i.value === val);
      if (match) {
        return match.label;
      }
    }
    return this.label();
  });

  protected readonly displayImage = computed(() => {
    const val = this.value();
    if (val) {
      const match = this.items().find((i) => i.value === val);
      if (match?.image) {
        return match.image;
      }
    }
    return null;
  });

  protected readonly displayIcon = computed((): IconProp | null => {
    const val = this.value();
    if (val) {
      const match = this.items().find((i) => i.value === val);
      if (match?.icon) {
        return match.icon;
      }
    }
    return this.icon() ?? null;
  });

  protected toggle(): void {
    this.isOpen.update((v) => !v);
  }

  protected onItemClick(item: DropdownMenuItem): void {
    if (item.disabled) {
      return;
    }
    this.itemSelected.emit(item.value);
    this.isOpen.set(false);
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
