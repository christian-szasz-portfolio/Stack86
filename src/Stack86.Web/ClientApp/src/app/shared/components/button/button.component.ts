import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';

export enum TooltipPosition {
  Center = 'center',
  Start = 'start',
  End = 'end',
}

export enum ButtonVariant {
  Default = 'default',
  Primary = 'primary',
  Success = 'success',
  Run = 'run',
  Step = 'step',
  Pause = 'pause',
  Reset = 'reset',
}

@Component({
  selector: 'emu-button',
  imports: [FaIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './button.component.html',
  styleUrl: './button.component.scss',
  host: {
    '[attr.variant]': 'variant()',
  },
})
export class ButtonComponent {
  protected readonly TooltipPosition = TooltipPosition;

  public readonly variant = input<ButtonVariant>(ButtonVariant.Default);
  public readonly icon = input<string>();
  public readonly disabled = input(false);
  public readonly loading = input(false);
  public readonly buttonTitle = input('');
  public readonly active = input(false);
  public readonly compact = input(false);
  public readonly tooltipPosition = input<TooltipPosition>(TooltipPosition.Center);

  public readonly pressed = output<void>();

  protected onButtonClick(): void {
    if (!this.disabled() && !this.loading()) {
      this.pressed.emit();
    }
  }
}
