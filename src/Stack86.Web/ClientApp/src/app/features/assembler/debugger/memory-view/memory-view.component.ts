import { ChangeDetectionStrategy, Component, computed, effect, ElementRef, inject, signal } from '@angular/core';
import { PanelHeaderComponent } from '@shared/components';
import { DebuggerPanelBase } from '../debugger-panel.base';
import { ExecutionStatus } from '../../../../core/emulator/execution/execution-result.model';

interface MemoryRow {
  address: string;
  bytes: string[];
  changed: boolean[];
  ascii: string;
}

@Component({
  selector: 'emu-memory-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [PanelHeaderComponent],
  templateUrl: './memory-view.component.html',
  styleUrl: './memory-view.component.scss',
})
export class MemoryViewComponent extends DebuggerPanelBase {
  public readonly panelTitle = 'Memory';
  public readonly startAddress = signal(0);
  public readonly addressInput = signal('0000');
  public readonly autoFollow = signal(true);
  private readonly elementRef = inject(ElementRef);
  private previousMemory: number[] = [];

  public constructor() {
    super();
    // Auto-navigate to the first modified memory region during and after execution
    effect(() => {
      const status = this.facade.executionStatus();
      this.facade.memory();
      if (!this.autoFollow()) return;
      if (status === ExecutionStatus.Running || status === ExecutionStatus.Halted || status === ExecutionStatus.Paused) {
        const addr = this.findFirstNonZero();
        if (addr !== null) {
          const visibleStart = this.startAddress();
          const visibleEnd = visibleStart + 16 * 16;
          if (addr < visibleStart || addr >= visibleEnd) {
            this.jumpTo(addr);
          }
        }
      }
    });

    // Scroll to first changed row after render
    effect(() => {
      this.rows();
      setTimeout(() => {
        const cell = this.elementRef.nativeElement.querySelector('.byte-col.changed') as HTMLElement;
        if (cell) {
          const row = cell.closest('.memory-row') as HTMLElement;
          row?.scrollIntoView({ block: 'nearest' });
        }
      });
    });
  }

  public readonly rows = computed<MemoryRow[]>(() => {
    const memory = this.facade.memory();
    const start = this.startAddress();
    const rows: MemoryRow[] = [];
    const rowCount = 16;

    for (let r = 0; r < rowCount; r++) {
      const addr = start + r * 16;
      const bytes: string[] = [];
      const changed: boolean[] = [];
      let ascii = '';

      for (let c = 0; c < 16; c++) {
        const idx = addr + c;
        const val = idx < memory.length ? memory[idx] : 0;
        const prev = idx < this.previousMemory.length ? this.previousMemory[idx] : 0;
        bytes.push(val.toString(16).toUpperCase().padStart(2, '0'));
        changed.push(this.previousMemory.length > 0 && val !== prev);
        ascii += val >= 32 && val <= 126 ? String.fromCharCode(val) : '.';
      }

      rows.push({
        address: addr.toString(16).toUpperCase().padStart(4, '0'),
        bytes,
        changed,
        ascii,
      });
    }

    this.previousMemory = [...memory];

    return rows;
  });

  public onAddressInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.addressInput.set(input.value);
  }

  public onAddressSubmit(): void {
    const parsed = parseInt(this.addressInput(), 16);
    if (!isNaN(parsed) && parsed >= 0 && parsed < 0x10000) {
      this.startAddress.set(parsed & 0xFFF0);
    }
  }

  public jumpTo(address: number): void {
    const aligned = address & 0xFFF0;
    this.startAddress.set(aligned);
    this.addressInput.set(aligned.toString(16).toUpperCase().padStart(4, '0'));
  }

  public jumpToStack(): void {
    const sp = this.facade.registers().sp;
    this.jumpTo(sp);
  }

  public jumpToData(): void {
    const addr = this.findFirstNonZero();
    if (addr !== null) {
      this.jumpTo(addr);
    }
  }

  private findFirstNonZero(): number | null {
    const memory = this.facade.memory();
    // Search below the stack area (0x0000–0xFF00)
    for (let i = 0; i < 0xFF00; i++) {
      if (memory[i] !== 0) {
        return i;
      }
    }
    return null;
  }
}