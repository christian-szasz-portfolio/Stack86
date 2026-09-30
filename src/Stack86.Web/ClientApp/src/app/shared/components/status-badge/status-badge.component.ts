import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'emu-status-badge',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss',
})
export class StatusBadgeComponent {
  public readonly status = input.required<string>();

  protected readonly statusClass = computed(() => `status-badge status-badge--${this.status()}`);
}
