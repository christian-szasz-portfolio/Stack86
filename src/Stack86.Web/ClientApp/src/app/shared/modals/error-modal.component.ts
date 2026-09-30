import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { ModalShellComponent } from './modal-shell.component';
import { ErrorBus } from '../../core/errors/error-bus.service';

/**
 * Renders the most recent error reported via the global ErrorBus as a friendly modal.
 * Mounted once at the application root.
 */
@Component({
  selector: 'emu-error-modal',
  imports: [ModalShellComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './error-modal.component.html',
  styleUrl: './error-modal.component.scss',
})
export class ErrorModalComponent {
  private readonly bus = inject(ErrorBus);

  protected readonly error = computed(() => this.bus.latest());

  protected onDismiss(): void {
    this.bus.dismiss();
  }
}
