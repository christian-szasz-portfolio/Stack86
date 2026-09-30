import { ChangeDetectionStrategy, Component, ElementRef, computed, effect, inject, signal, viewChild } from '@angular/core';
import { EmptyStateComponent, ConsoleTabBarComponent, ConsoleTab, ConsoleTabBadge, ConsoleSeverity } from '@shared/components';
import { EmulatorFacade } from '../../../state/emulator.facade';
import { ExecutionStatus } from '../../../core/emulator/execution/execution-result.model';

@Component({
  selector: 'emu-console',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ConsoleTabBarComponent, EmptyStateComponent],
  templateUrl: './console.component.html',
  styleUrl: './console.component.scss',
})
export class ConsoleComponent {
  protected readonly ConsoleTab = ConsoleTab;

  public readonly facade = inject(EmulatorFacade);
  public readonly activeTab = signal(ConsoleTab.Output);

  private readonly scrollContainer = viewChild<ElementRef<HTMLElement>>('scrollContainer');

  public readonly outputLines = computed(() =>
    this.facade.consoleOutput().filter((l) => !l.startsWith('[Info]') && !l.startsWith('[Warn]') && !l.startsWith('[Error]')),
  );

  public readonly problemLines = computed(() =>
    this.facade.consoleOutput().filter((l) => l.startsWith('[Error]') || l.startsWith('[Warn]')),
  );

  public readonly buildLogLines = computed(() =>
    this.facade.consoleOutput().filter((l) => l.startsWith('[Info]')),
  );

  public readonly badges = computed<ConsoleTabBadge[]>(() => {
    const errors = this.facade.consoleOutput().filter((l) => l.startsWith('[Error]')).length;
    const warnings = this.facade.consoleOutput().filter((l) => l.startsWith('[Warn]')).length;
    const result: ConsoleTabBadge[] = [];
    if (errors > 0) {
      result.push({ tab: ConsoleTab.Problems, count: errors, severity: ConsoleSeverity.Error });
    } else if (warnings > 0) {
      result.push({ tab: ConsoleTab.Problems, count: warnings, severity: ConsoleSeverity.Warning });
    }
    return result;
  });

  public constructor() {
    effect(() => {
      this.facade.consoleOutput();
      const el = this.scrollContainer()?.nativeElement;
      if (el) {
        requestAnimationFrame(() => el.scrollTop = el.scrollHeight);
      }
    });

    effect(() => {
      const status = this.facade.executionStatus();
      const output = this.facade.consoleOutput();

      // Program started running → Build Log
      if (status === ExecutionStatus.Running) {
        this.activeTab.set(ConsoleTab.BuildLog);
        return;
      }

      // Program finished (halted/error handled below) → Output
      if (status === ExecutionStatus.Halted) {
        this.activeTab.set(ConsoleTab.Output);
        return;
      }

      if (output.length === 0) {
        return;
      }
      const last = output[output.length - 1];
      if (last.startsWith('[Error]') || last.startsWith('[Warn]')) {
        this.activeTab.set(ConsoleTab.Problems);
      } else if (last.startsWith('[Info]')) {
        this.activeTab.set(ConsoleTab.BuildLog);
      } else {
        this.activeTab.set(ConsoleTab.Output);
      }
    });
  }
}