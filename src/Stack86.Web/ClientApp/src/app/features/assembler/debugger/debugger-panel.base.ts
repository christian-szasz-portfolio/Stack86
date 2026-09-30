import { inject } from '@angular/core';
import { EmulatorFacade } from '../../../state/emulator.facade';

export abstract class DebuggerPanelBase {
  protected readonly facade = inject(EmulatorFacade);

  public abstract readonly panelTitle: string;
}
