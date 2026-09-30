import { ChangeDetectionStrategy, Component, computed, signal } from '@angular/core';
import { PanelHeaderComponent, SettingsDropdownComponent, SettingsOption } from '@shared/components';
import { DebuggerPanelBase } from '../debugger-panel.base';

interface RegisterEntry {
  name: string;
  hex: string;
  bin: string;
  dec: number;
  changed: boolean;
}

@Component({
  selector: 'emu-registers',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PanelHeaderComponent, SettingsDropdownComponent],
  templateUrl: './registers.component.html',
  styleUrl: './registers.component.scss',
})
export class RegistersComponent extends DebuggerPanelBase {
  public readonly panelTitle = 'Registers';

  public readonly showHex = signal(localStorage.getItem('stack86_registers_showHex') !== 'false');
  public readonly showBinary = signal(localStorage.getItem('stack86_registers_showBinary') === 'true');

  public readonly settingsOptions = computed<SettingsOption[]>(() => [
    { label: 'Hex', checked: this.showHex() },
    { label: 'Binary', checked: this.showBinary() },
  ]);

  public readonly gridColumns = computed(() => {
    const cols = ['auto']; // name
    if (this.showHex()) cols.push('1fr');
    cols.push('1fr'); // dec always visible
    if (this.showBinary()) cols.push('1fr');
    return cols.join(' ');
  });

  /** Number of columns per row, used for alternating-row nth-child calculation */
  public readonly colCount = computed(() => {
    let count = 2; // name + dec
    if (this.showHex()) count++;
    if (this.showBinary()) count++;
    return count;
  });

  private previousRegisters: Record<string, number> = {};

  public readonly entries = computed<RegisterEntry[]>(() => {
    const regs = this.facade.registers();
    const names: (keyof typeof regs)[] = ['ax', 'bx', 'cx', 'dx', 'sp', 'bp', 'si', 'di', 'ip', 'cs', 'ds', 'es', 'ss'];
    const result = names.map((name) => {
      const value = regs[name];
      const changed = this.previousRegisters[name] !== undefined && this.previousRegisters[name] !== value;
      return {
        name: name.toUpperCase(),
        hex: value.toString(16).toUpperCase().padStart(4, '0') + 'h',
        bin: value.toString(2).padStart(16, '0'),
        dec: value,
        changed,
      };
    });
    this.previousRegisters = { ...regs };
    return result;
  });

  public onSettingToggled(event: { index: number; checked: boolean }): void {
    if (event.index === 0) {
      this.showHex.set(event.checked);
      localStorage.setItem('stack86_registers_showHex', String(event.checked));
    }
    if (event.index === 1) {
      this.showBinary.set(event.checked);
      localStorage.setItem('stack86_registers_showBinary', String(event.checked));
    }
  }
}