import { describe, it, expect } from 'vitest';
import {
  SupportedLanguage,
  LANGUAGE_OPTIONS,
  getMainFileName,
  createMainFile,
} from './compiler.models';

describe('Compiler Models', () => {
  describe('SupportedLanguage', () => {
    it('should have C as "c"', () => {
      expect(SupportedLanguage.C).toBe('c');
    });

    it('should have Cpp as "cpp"', () => {
      expect(SupportedLanguage.Cpp).toBe('cpp');
    });

    it('should have CSharp as "csharp"', () => {
      expect(SupportedLanguage.CSharp).toBe('csharp');
    });

    it('should have JavaScript as "javascript"', () => {
      expect(SupportedLanguage.JavaScript).toBe('javascript');
    });

    it('should have TypeScript as "typescript"', () => {
      expect(SupportedLanguage.TypeScript).toBe('typescript');
    });





    it('should have exactly 5 members', () => {
      const values = Object.values(SupportedLanguage);
      expect(values).toHaveLength(5);
    });
  });

  describe('LANGUAGE_OPTIONS', () => {
    it('should contain 5 language options', () => {
      expect(LANGUAGE_OPTIONS).toHaveLength(5);
    });

    it('should offer exactly the demo languages', () => {
      expect(LANGUAGE_OPTIONS.map((l) => l.id)).toEqual([
        SupportedLanguage.C,
        SupportedLanguage.Cpp,
        SupportedLanguage.CSharp,
        SupportedLanguage.JavaScript,
        SupportedLanguage.TypeScript,
      ]);
    });

    it('should have a label for every language', () => {
      for (const lang of LANGUAGE_OPTIONS) {
        expect(lang.label).toBeTruthy();
      }
    });

    it('should have a monacoLanguage for every language', () => {
      for (const lang of LANGUAGE_OPTIONS) {
        expect(lang.monacoLanguage).toBeTruthy();
      }
    });

    it('should have unique ids', () => {
      const ids = LANGUAGE_OPTIONS.map((l) => l.id);
      expect(new Set(ids).size).toBe(ids.length);
    });

    it('should have a mainFileName for every language', () => {
      for (const lang of LANGUAGE_OPTIONS) {
        expect(lang.mainFileName).toBeTruthy();
      }
    });
  });

  describe('getMainFileName', () => {
    it('should return main.c for C', () => {
      expect(getMainFileName(SupportedLanguage.C)).toBe('main.c');
    });

    it('should return main.cpp for C++', () => {
      expect(getMainFileName(SupportedLanguage.Cpp)).toBe('main.cpp');
    });
  });

  describe('createMainFile', () => {
    it('should create a main file for a given language', () => {
      const file = createMainFile(SupportedLanguage.Cpp, 'int main() {}');
      expect(file.name).toBe('main.cpp');
      expect(file.content).toBe('int main() {}');
      expect(file.isMain).toBe(true);
      expect(file.id).toBeTruthy();
    });
  });
});
