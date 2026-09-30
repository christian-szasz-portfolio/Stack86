import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { SelectOption } from './select.models';

@Component({
  selector: 'emu-select',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './select.component.html',
  styleUrl: './select.component.scss',
})
export class SelectComponent {
  public readonly options = input.required<SelectOption[]>();
  public readonly value = input('');
  public readonly placeholder = input('');

  public readonly valueChange = output<string>();

  protected onChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.valueChange.emit(select.value);
  }
}
