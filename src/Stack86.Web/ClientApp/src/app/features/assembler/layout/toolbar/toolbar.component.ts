import { ChangeDetectionStrategy, Component, computed, inject, input, model, signal } from '@angular/core';
import { ButtonComponent, ButtonVariant, DropdownMenuComponent, DropdownMenuItem, SelectComponent, SelectOption, StatusBadgeComponent, TooltipPosition } from '@shared/components';
import { EmulatorFacade } from '../../../../state/emulator.facade';
import { ExecutionStatus } from '../../../../core/emulator/execution/execution-result.model';
import { SAMPLE_PROGRAMS, SampleProgram } from './sample-programs';
import { FileMenuComponent } from './file-menu/file-menu.component';

export enum SpeedUnit {
  Ms = 'ms',
  S = 's',
}

@Component({
  selector: 'emu-toolbar',
  imports: [ButtonComponent, DropdownMenuComponent, SelectComponent, StatusBadgeComponent, FileMenuComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './toolbar.component.html',
  styleUrl: './toolbar.component.scss',
  host: {
    '[class.compact]': 'compact()',
  },
})
export class ToolbarComponent {
  public readonly facade = inject(EmulatorFacade);
  public readonly samples: SampleProgram[] = SAMPLE_PROGRAMS;

  public readonly compact = input(false);
  public readonly TooltipPosition = TooltipPosition;
  public readonly ButtonVariant = ButtonVariant;
  public readonly showDataFlow = model(false);

  public readonly sampleItems: DropdownMenuItem[] = SAMPLE_PROGRAMS.map((s, i) => ({
    value: i.toString(),
    label: s.name,
  }));

  public readonly selectedSample = signal('');

  public readonly status = computed(() => this.facade.executionStatus());
  public readonly isRunning = computed(() => this.status() === ExecutionStatus.Running);
  public readonly canRun = computed(() => {
    const s = this.status();
    return s === ExecutionStatus.Idle || s === ExecutionStatus.Paused;
  });
  public readonly canStep = computed(() => this.canRun());
  public readonly canPause = computed(() => this.isRunning());
  public readonly canExportCom = computed(() => {
    const s = this.status();
    return s !== ExecutionStatus.Idle;
  });

  // Speed control — defaults to a 1s step delay so the data-flow animation is
  // visible out of the box (see the constructor, which syncs the facade).
  public readonly speedUnit = signal<SpeedUnit>(SpeedUnit.S);
  public readonly speedValue = signal(1);
  public readonly speedMax = computed(() => this.speedUnit() === SpeedUnit.S ? 10 : 500);
  public readonly speedStep = computed(() => this.speedUnit() === SpeedUnit.S ? 1 : 10);
  public readonly speedLabel = computed(() => {
    const val = this.speedValue();
    if (val === 0) return 'Full Speed';
    return `${val}${this.speedUnit()}`;
  });

  public readonly unitOptions: SelectOption[] = [
    { value: SpeedUnit.Ms, label: 'ms' },
    { value: SpeedUnit.S, label: 's' },
  ];

  public constructor() {
    // Apply the default 1s step delay to the facade on load.
    this.facade.setExecutionDelay(this.speedValue() * 1000);
  }

  public assemble(): void {
    const source = this.facade.sourceCode();
    this.facade.loadProgram(source);
  }

  public run(): void {
    this.facade.run();
  }

  public step(): void {
    this.facade.step();
  }

  public pause(): void {
    this.facade.pause();
  }

  public reset(): void {
    this.facade.reset();
  }

  public loadSample(value: string): void {
    const idx = parseInt(value, 10);
    if (!isNaN(idx) && idx >= 0 && idx < this.samples.length) {
      this.facade.updateSourceCode(this.samples[idx].source);
      this.facade.reset();
      this.selectedSample.set(value);
    }
  }

  public onSpeedValueChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = parseInt(input.value, 10);
    this.speedValue.set(val);
    this.updateDelayMs();
  }

  public onSpeedUnitChange(value: string): void {
    this.speedUnit.set(value as SpeedUnit);
    // Clamp value to new max
    const max = value === SpeedUnit.S ? 10 : 2000;
    if (this.speedValue() > max) {
      this.speedValue.set(max);
    }
    this.updateDelayMs();
  }

  public onFileMenuAction(action: string): void {
    switch (action) {
      case 'importCom': this.facade.importCom(); break;
      case 'exportCom': this.facade.exportCom(); break;
      case 'importAsm': this.facade.importAsm(); break;
      case 'exportAsm': this.facade.exportAsm(); break;
    }
  }

  private updateDelayMs(): void {
    const val = this.speedValue();
    const ms = this.speedUnit() === SpeedUnit.S ? val * 1000 : val;
    this.facade.setExecutionDelay(ms);
  }

  public toggleDataFlow(): void {
    this.showDataFlow.update((v) => !v);
  }
}