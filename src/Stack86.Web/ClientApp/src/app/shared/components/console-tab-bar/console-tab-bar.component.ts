import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { ConsoleTab, ConsoleTabBadge, ConsoleSeverity } from './console-tab-bar.models';

@Component({
  selector: 'emu-console-tab-bar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './console-tab-bar.component.html',
  styleUrl: './console-tab-bar.component.scss',
})
export class ConsoleTabBarComponent {
  protected readonly ConsoleSeverity = ConsoleSeverity;

  private static readonly defaultTabs: { id: ConsoleTab; label: string }[] = [
    { id: ConsoleTab.Output, label: 'Output' },
    { id: ConsoleTab.Problems, label: 'Problems' },
    { id: ConsoleTab.BuildLog, label: 'Build Log' },
  ];

  public readonly activeTab = model.required<ConsoleTab>();
  public readonly badges = input<ConsoleTabBadge[]>([]);
  public readonly tabs = input<{ id: ConsoleTab; label: string }[]>(ConsoleTabBarComponent.defaultTabs);

  protected getBadge(tabId: ConsoleTab): ConsoleTabBadge | undefined {
    return this.badges().find((b) => b.tab === tabId && b.count > 0);
  }

  protected selectTab(tab: ConsoleTab): void {
    this.activeTab.set(tab);
  }
}
