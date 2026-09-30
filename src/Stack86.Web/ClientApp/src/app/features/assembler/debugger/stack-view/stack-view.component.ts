import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, signal } from '@angular/core';
import { PanelHeaderComponent, EmptyStateComponent, SettingsDropdownComponent, SettingsOption } from '@shared/components';
import { DebuggerPanelBase } from '../debugger-panel.base';
import { ExecutionStatus } from '../../../../core/emulator/execution/execution-result.model';

interface StackDisplayEntry {
  address: string;
  value: string;
  valueBin: string;
  isSp: boolean;
  changed: boolean;
}

@Component({
  selector: 'emu-stack-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PanelHeaderComponent, EmptyStateComponent, SettingsDropdownComponent],
  templateUrl: './stack-view.component.html',
  styleUrl: './stack-view.component.scss',
})
export class StackViewComponent extends DebuggerPanelBase {
  public readonly panelTitle = 'Stack';

  public readonly showHex = signal(localStorage.getItem('stack86_stack_showHex') !== 'false');
  public readonly showBinary = signal(localStorage.getItem('stack86_stack_showBinary') === 'true');
  private readonly elementRef = inject(ElementRef);

  public constructor() {
    super();
    // Scroll to the current SP row after render
    effect(() => {
      this.entries();
      setTimeout(() => {
        const row = this.elementRef.nativeElement.querySelector('.current-sp') as HTMLElement;
        row?.scrollIntoView({ block: 'nearest' });
      });
    });
  }

  public readonly settingsOptions = computed<SettingsOption[]>(() => [
    { label: 'Hex', checked: this.showHex() },
    { label: 'Binary', checked: this.showBinary() },
  ]);

  private previousValues: Record<string, number> = {};
  private readonly highlightedAddresses = new Set<string>();
  private tracking = false;

  public readonly sp = computed(() => this.facade.registers().sp);

  public readonly entries = computed<StackDisplayEntry[]>(() => {
    const status = this.facade.executionStatus();
    const isExecuting = status === ExecutionStatus.Running
      || status === ExecutionStatus.Paused
      || status === ExecutionStatus.Halted;

    // Reset tracking state when execution resets to Idle
    if (!isExecuting) {
      this.highlightedAddresses.clear();
      this.previousValues = {};
      this.tracking = false;
    }

    const currentAddresses = new Set<string>();
    const result = this.facade.stackView().map((entry) => {
      const addrKey = entry.address.toString(16);
      currentAddresses.add(addrKey);
      if (this.tracking) {
        if (entry.value === 0) {
          this.highlightedAddresses.delete(addrKey);
        } else {
          const prev = this.previousValues[addrKey];
          const isNew = prev === undefined;
          const valueChanged = !isNew && prev !== entry.value;
          if (isNew || valueChanged) {
            this.highlightedAddresses.add(addrKey);
          }
        }
      }
      return {
        address: entry.address.toString(16).toUpperCase().padStart(4, '0'),
        value: entry.value.toString(16).toUpperCase().padStart(4, '0'),
        valueBin: entry.value.toString(2).padStart(16, '0'),
        isSp: entry.address === this.sp(),
        changed: this.highlightedAddresses.has(addrKey),
      };
    });
    // Remove highlights for addresses that were popped (no longer in stack)
    for (const addr of this.highlightedAddresses) {
      if (!currentAddresses.has(addr)) {
        this.highlightedAddresses.delete(addr);
      }
    }
    this.previousValues = {};
    for (const entry of this.facade.stackView()) {
      this.previousValues[entry.address.toString(16)] = entry.value;
    }
    // Start tracking after the first evaluation during execution
    if (isExecuting) {
      this.tracking = true;
    }
    return result;
  });

  public onSettingToggled(event: { index: number; checked: boolean }): void {
    if (event.index === 0) {
      this.showHex.set(event.checked);
      localStorage.setItem('stack86_stack_showHex', String(event.checked));
    }
    if (event.index === 1) {
      this.showBinary.set(event.checked);
      localStorage.setItem('stack86_stack_showBinary', String(event.checked));
    }
  }
}