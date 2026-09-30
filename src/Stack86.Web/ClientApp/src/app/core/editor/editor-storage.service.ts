import { Service, inject } from '@angular/core';
import { EditorDatabaseService } from './editor-database.service';
import { CompilerFile } from '../compiler/compiler.models';

const COMPILER_PROJECT_KEY = 'compiler-project';
const ASSEMBLER_SOURCE_KEY = 'assembler-source';

@Service()
export class EditorStorageService {
  private readonly db = inject(EditorDatabaseService);
  private compilerHydrated = false;

  public get isCompilerHydrated(): boolean {
    return this.compilerHydrated;
  }

  public markCompilerHydrated(): void {
    this.compilerHydrated = true;
  }

  public async saveCompilerProject(files: CompilerFile[]): Promise<void> {
    await this.db.save(COMPILER_PROJECT_KEY, JSON.stringify(files));
  }

  public async loadCompilerProject(): Promise<CompilerFile[] | null> {
    const raw = await this.db.load(COMPILER_PROJECT_KEY);
    if (!raw) {
      return null;
    }
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed) || parsed.length === 0) {
      return null;
    }
    return parsed as CompilerFile[];
  }

  public async saveAssemblerSource(code: string): Promise<void> {
    await this.db.save(ASSEMBLER_SOURCE_KEY, code);
  }

  public async loadAssemblerSource(): Promise<string | null> {
    return this.db.load(ASSEMBLER_SOURCE_KEY);
  }
}
