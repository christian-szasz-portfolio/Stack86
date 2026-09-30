import { Service } from '@angular/core';
import { SupportedLanguage } from './compiler.models';

export interface AssemblerSourceOrigin {
  language: SupportedLanguage;
  sampleName: string | null;
}

@Service()
export class AssemblerBridgeService {
  private pendingAssembly: string | null = null;
  private sourceOrigin: AssemblerSourceOrigin | null = null;

  public loadAssemblyIntoAssembler(assembly: string, origin: AssemblerSourceOrigin): void {
    this.pendingAssembly = assembly;
    this.sourceOrigin = origin;
  }

  public consumePendingAssembly(): string | null {
    const assembly = this.pendingAssembly;
    this.pendingAssembly = null;
    return assembly;
  }

  public getSourceOrigin(): AssemblerSourceOrigin | null {
    return this.sourceOrigin;
  }
}
