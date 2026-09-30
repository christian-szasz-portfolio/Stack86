import { ChangeDetectionStrategy, Component, computed } from '@angular/core';
import { PanelHeaderComponent } from '@shared/components';
import { DebuggerPanelBase } from '../debugger-panel.base';

@Component({
  selector: 'emu-flags',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PanelHeaderComponent],
  templateUrl: './flags.component.html',
  styleUrl: './flags.component.scss',
})
export class FlagsComponent extends DebuggerPanelBase {
  public readonly panelTitle = 'Flags';

  public readonly flagEntries = computed(() => {
    const flags = this.facade.flags();
    return [
      { name: 'ZF', set: flags.zero },
      { name: 'CF', set: flags.carry },
      { name: 'SF', set: flags.sign },
      { name: 'OF', set: flags.overflow },
    ];
  });
}