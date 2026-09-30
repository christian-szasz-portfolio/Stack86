import { ChangeDetectionStrategy, Component, computed, effect, inject, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { EmptyStateComponent, ConsoleTabBarComponent, ConsoleTab, ConsoleTabBadge, ConsoleSeverity } from '@shared/components';
import { CompilerFacade } from '../../../state/compiler.facade';

export enum ConsoleLineType {
  Info = 'info',
  Error = 'error',
  Warning = 'warning',
}

interface ConsoleLine {
  type: ConsoleLineType;
  text: string;
}

@Component({
  selector: 'emu-compile-console',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ConsoleTabBarComponent, EmptyStateComponent, FaIconComponent],
  templateUrl: './compile-console.component.html',
  styleUrl: './compile-console.component.scss',
})
export class CompileConsoleComponent {
  protected readonly ConsoleTab = ConsoleTab;
  protected readonly ConsoleLineType = ConsoleLineType;

  protected readonly consoleTabs = [
    { id: ConsoleTab.Problems, label: 'Problems' },
    { id: ConsoleTab.BuildLog, label: 'Build Log' },
  ];

  private readonly facade = inject(CompilerFacade);

  public readonly loading = this.facade.loading;
  public readonly activeTab = signal(ConsoleTab.BuildLog);

  public readonly buildLogLines = computed<ConsoleLine[]>(() => {
    const result: ConsoleLine[] = [];
    for (const msg of this.facade.consoleMessages()) {
      if (msg.startsWith('[Error]')) {
        result.push({ type: ConsoleLineType.Error, text: msg });
      } else if (msg.startsWith('[Warn]')) {
        result.push({ type: ConsoleLineType.Warning, text: msg });
      } else {
        result.push({ type: ConsoleLineType.Info, text: msg });
      }
    }
    return result;
  });

  public readonly problemLines = computed<ConsoleLine[]>(() => {
    const result: ConsoleLine[] = [];

    const compilationError = this.facade.compilationError();
    if (compilationError) {
      result.push({ type: ConsoleLineType.Error, text: compilationError });
    }

    for (const err of this.facade.errors()) {
      const location = err.file ? `${err.file}:${err.line}:${err.column}` : `${err.line}:${err.column}`;
      result.push({ type: ConsoleLineType.Error, text: `Error (${location}): ${err.message}` });
    }

    for (const warn of this.facade.warnings()) {
      const location = warn.file ? `${warn.file}:${warn.line}:${warn.column}` : `${warn.line}:${warn.column}`;
      result.push({ type: ConsoleLineType.Warning, text: `Warning (${location}): ${warn.message}` });
    }

    return result;
  });

  public readonly errorCount = computed(() => {
    const compilationError = this.facade.compilationError();
    return this.facade.errors().length + (compilationError ? 1 : 0);
  });

  public readonly warningCount = computed(() => this.facade.warnings().length);

  public readonly badges = computed<ConsoleTabBadge[]>(() => {
    const result: ConsoleTabBadge[] = [];
    const errors = this.errorCount();
    const warnings = this.warningCount();
    if (errors > 0) {
      result.push({ tab: ConsoleTab.Problems, count: errors, severity: ConsoleSeverity.Error });
    } else if (warnings > 0) {
      result.push({ tab: ConsoleTab.Problems, count: warnings, severity: ConsoleSeverity.Warning });
    }
    return result;
  });

  public constructor() {
    effect(() => {
      if (this.loading()) {
        this.activeTab.set(ConsoleTab.BuildLog);
      }
    });

    effect(() => {
      if (!this.loading() && (this.errorCount() > 0 || this.warningCount() > 0)) {
        this.activeTab.set(ConsoleTab.Problems);
      }
    });
  }
}
