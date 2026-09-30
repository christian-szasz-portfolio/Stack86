import { SupportedLanguage } from './compiler.models';

/**
 * Persists and restores the last selected compiler options (language + example index)
 * in `localStorage` so they survive page reloads.
 */
export class CompilerStorageUtility {
  private static readonly LANGUAGE_KEY = 'compiler.selectedLanguage';
  private static readonly SAMPLE_KEY = 'compiler.selectedSample';

  public static saveLanguage(language: SupportedLanguage): void {
    localStorage.setItem(CompilerStorageUtility.LANGUAGE_KEY, language);
  }

  public static loadLanguage(): SupportedLanguage | null {
    const raw = localStorage.getItem(CompilerStorageUtility.LANGUAGE_KEY);
    if (raw !== null && Object.values(SupportedLanguage).includes(raw as SupportedLanguage)) {
      return raw as SupportedLanguage;
    }
    return null;
  }

  public static saveSample(language: SupportedLanguage, sampleIndex: number): void {
    localStorage.setItem(CompilerStorageUtility.SAMPLE_KEY, `${language}:${sampleIndex}`);
  }

  public static loadSample(language: SupportedLanguage): number | null {
    const raw = localStorage.getItem(CompilerStorageUtility.SAMPLE_KEY);
    if (raw === null) {
      return null;
    }
    const [lang, idx] = raw.split(':');
    if (lang !== language) {
      return null;
    }
    const parsed = parseInt(idx, 10);
    return isNaN(parsed) ? null : parsed;
  }
}
