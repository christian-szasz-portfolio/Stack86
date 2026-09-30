import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';

/**
 * Reusable modal shell with header, body slot, footer slot, and a close button.
 */
@Component({
  selector: 'emu-modal-shell',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './modal.styles.scss',
  templateUrl: './modal-shell.component.html',
})
export class ModalShellComponent {
  public readonly title = input.required<string>();
  public readonly showClose = input<boolean>(true);
  public readonly closeOnBackdrop = input<boolean>(true);
  public readonly dismiss = output<void>();

  /**
   * Dismisses only when the backdrop itself was clicked. Clicks inside the dialog bubble up to
   * here with a different target, which is why the shell no longer needs its own
   * stopPropagation handler — that handler existed purely to block this one, and a click
   * binding with no real interaction behind it failed the template accessibility rules.
   */
  protected onBackdropClick(event: MouseEvent): void {
    if (this.closeOnBackdrop() && event.target === event.currentTarget) {
      this.dismiss.emit();
    }
  }

  /** Keyboard equivalent of a backdrop click, so the modal is dismissible without a mouse. */
  protected onEscape(): void {
    if (this.closeOnBackdrop()) {
      this.dismiss.emit();
    }
  }
}
