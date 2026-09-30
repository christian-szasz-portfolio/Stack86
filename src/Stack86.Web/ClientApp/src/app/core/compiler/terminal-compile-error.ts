/** Thrown to signal that an HTTP error from the streaming endpoint is non-retriable (e.g. 4xx). */
export class TerminalCompileError extends Error {
  public constructor(message: string) {
    super(message);
    this.name = 'TerminalCompileError';
  }
}
