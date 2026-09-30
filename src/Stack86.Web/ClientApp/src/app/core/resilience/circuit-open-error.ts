/** Thrown by `CompilerService.compileStream` when the client-side circuit breaker is open. */
export class CircuitOpenError extends Error {
  public readonly remainingCooldownMs: number;

  public constructor(remainingCooldownMs: number) {
    super('Compiler temporarily unavailable. Please retry shortly.');
    this.name = 'CircuitOpenError';
    this.remainingCooldownMs = remainingCooldownMs;
  }
}
