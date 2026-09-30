import { describe, it, expect } from 'vitest';
import { ConsoleTab, ConsoleTabBadge, ConsoleSeverity } from './console-tab-bar.models';

describe('ConsoleTabBar models', () => {
  it('should define three tab values', () => {
    expect(ConsoleTab.Output).toBe('output');
    expect(ConsoleTab.Problems).toBe('problems');
    expect(ConsoleTab.BuildLog).toBe('build-log');
  });

  it('should create a badge with error severity', () => {
    const badge: ConsoleTabBadge = { tab: ConsoleTab.Problems, count: 3, severity: ConsoleSeverity.Error };
    expect(badge.count).toBe(3);
    expect(badge.severity).toBe(ConsoleSeverity.Error);
  });

  it('should create a badge with warning severity', () => {
    const badge: ConsoleTabBadge = { tab: ConsoleTab.Problems, count: 1, severity: ConsoleSeverity.Warning };
    expect(badge.count).toBe(1);
    expect(badge.severity).toBe(ConsoleSeverity.Warning);
  });
});
