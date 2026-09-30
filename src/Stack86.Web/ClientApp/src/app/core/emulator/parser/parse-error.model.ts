export enum ParseErrorSeverity {
  Error = 'error',
  Warning = 'warning',
}

export class ParseError {
  public constructor(
    public readonly line: number,
    public readonly column: number,
    public readonly message: string,
    public readonly severity: ParseErrorSeverity = ParseErrorSeverity.Error,
  ) {}

  public toString(): string {
    return `[${this.severity}] Line ${this.line + 1}, Col ${this.column + 1}: ${this.message}`;
  }
}
