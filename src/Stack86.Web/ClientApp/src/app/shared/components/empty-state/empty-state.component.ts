import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

@Component({
  selector: 'emu-empty-state',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
})
export class EmptyStateComponent {
  public readonly message = input.required<string>();
  public readonly icon = input<string>();
}
