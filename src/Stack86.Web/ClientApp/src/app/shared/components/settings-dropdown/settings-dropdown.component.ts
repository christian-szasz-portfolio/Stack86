import { ChangeDetectionStrategy, Component, ElementRef, inject, input, output, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { SettingsOption } from './settings-dropdown.models';

@Component({
  host: {
    '(document:click)': 'onDocumentClick($event)',
  },
  selector: 'emu-settings-dropdown',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings-dropdown.component.html',
  styleUrl: './settings-dropdown.component.scss',
})
export class SettingsDropdownComponent {
  private readonly elementRef = inject(ElementRef);

  public readonly options = input.required<SettingsOption[]>();
  public readonly optionToggled = output<{ index: number; checked: boolean }>();

  public readonly isOpen = signal(false);

  public toggle(): void {
    this.isOpen.update((v) => !v);
  }

  public onCheckboxChange(index: number, event: Event): void {
    const checkbox = event.target as HTMLInputElement;
    this.optionToggled.emit({ index, checked: checkbox.checked });
  }

  public onDocumentClick(event: MouseEvent): void {
    if (this.isOpen() && !this.elementRef.nativeElement.contains(event.target)) {
      this.isOpen.set(false);
    }
  }
}
