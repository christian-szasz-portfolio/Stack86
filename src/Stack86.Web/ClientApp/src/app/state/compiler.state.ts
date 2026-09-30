import { SupportedLanguage, CompilationDiagnostic, CompilerFile, createMainFile, getMainFileName } from '../core/compiler/compiler.models';
import { CompilerStorageUtility } from '../core/compiler/compiler-storage.utility';
import { getDefaultSample, getSamplesForLanguage } from '../features/compiler/compile-toolbar/sample-programs';

export const COMPILER_FEATURE_KEY = 'compiler';

export interface CompilerState {
  selectedLanguage: SupportedLanguage;
  files: CompilerFile[];
  activeFileId: string;
  loading: boolean;
  assembly: string | null;
  errors: CompilationDiagnostic[];
  warnings: CompilationDiagnostic[];
  compilationError: string | null;
  consoleMessages: string[];
  circuitOpen: boolean;
  circuitCooldownMs: number;
}

export const createInitialFiles = (language: SupportedLanguage): CompilerFile[] => {
  const sample = getDefaultSample(language);
  const mainContent = sample?.files[0]?.content ?? '';
  return [createMainFile(language, mainContent)];
}

const createRestoredFiles = (language: SupportedLanguage): CompilerFile[] => {
  const sampleIndex = CompilerStorageUtility.loadSample(language);
  if (sampleIndex !== null) {
    const samples = getSamplesForLanguage(language);
    if (sampleIndex >= 0 && sampleIndex < samples.length) {
      const sample = samples[sampleIndex];
      const mainFileName = getMainFileName(language);
      return sample.files.map((f) => ({
        id: crypto.randomUUID(),
        name: f.name,
        content: f.content,
        isMain: f.name === mainFileName,
      }));
    }
  }
  return createInitialFiles(language);
}

const restoredLanguage = CompilerStorageUtility.loadLanguage() ?? SupportedLanguage.C;
const defaultFiles = createRestoredFiles(restoredLanguage);

export const initialCompilerState: CompilerState = {
  selectedLanguage: restoredLanguage,
  files: defaultFiles,
  activeFileId: defaultFiles[0].id,
  loading: false,
  assembly: null,
  errors: [],
  warnings: [],
  compilationError: null,
  consoleMessages: [],
  circuitOpen: false,
  circuitCooldownMs: 0,
};
