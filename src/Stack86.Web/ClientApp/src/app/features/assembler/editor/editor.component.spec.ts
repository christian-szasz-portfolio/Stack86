import { describe, it, expect } from 'vitest';
import { ASM_8086_LANGUAGE_ID, asm8086LanguageDef, asm8086ThemeDef } from './monaco-8086-language';

/**
 * Tests the Monaco 8086 language definition and theme configuration.
 */

describe('Monaco 8086 Language', () => {
  describe('language ID', () => {
    it('should be a non-empty string', () => {
      expect(ASM_8086_LANGUAGE_ID).toBeTruthy();
      expect(typeof ASM_8086_LANGUAGE_ID).toBe('string');
    });
  });

  describe('language definition (Monarch tokenizer)', () => {
    it('should define tokenizer with root state', () => {
      expect(asm8086LanguageDef.tokenizer).toBeDefined();
      expect(asm8086LanguageDef.tokenizer['root']).toBeDefined();
      expect(Array.isArray(asm8086LanguageDef.tokenizer['root'])).toBe(true);
    });

    it('should have root rules', () => {
      const root = asm8086LanguageDef.tokenizer['root'];
      expect(root.length).toBeGreaterThan(0);
    });

    it('should be case-insensitive', () => {
      expect(asm8086LanguageDef.ignoreCase).toBe(true);
    });
  });

  describe('theme definition', () => {
    it('should have a base theme', () => {
      expect(asm8086ThemeDef.base).toBeDefined();
    });

    it('should inherit from base', () => {
      expect(asm8086ThemeDef.inherit).toBe(true);
    });

    it('should define rules', () => {
      expect(Array.isArray(asm8086ThemeDef.rules)).toBe(true);
      expect(asm8086ThemeDef.rules.length).toBeGreaterThan(0);
    });

    it('should define colors', () => {
      expect(asm8086ThemeDef.colors).toBeDefined();
    });
  });
});
