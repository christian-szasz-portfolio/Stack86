import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

@Component({
  selector: 'emu-panel-header',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './panel-header.component.html',
  styleUrl: './panel-header.component.scss',
})
export class PanelHeaderComponent {
  public readonly title = input.required<string>();
  public readonly icon = input<string>();
}
