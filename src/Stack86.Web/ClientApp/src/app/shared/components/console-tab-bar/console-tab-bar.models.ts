export enum ConsoleTab {
  BuildLog = 'build-log',
  Problems = 'problems',
  Output = 'output',
}

export enum ConsoleSeverity {
  Error = 'error',
  Warning = 'warning',
  Info = 'info',
}

export interface ConsoleTabBadge {
  tab: ConsoleTab;
  count: number;
  severity: ConsoleSeverity;
}
