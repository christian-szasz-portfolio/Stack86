import { describe, expect, it } from 'vitest';
import { SupportedLanguage } from '../../../../core/compiler/compiler.models';
import {
  buildFileMenu,
  EDIT_MENU,
  NEW_FILE_ACTION_PREFIX,
} from './menu-definitions';

describe('menu-definitions', () => {
  describe('buildFileMenu', () => {
    it('builds a File menu including a header item for languages that have one (C)', () => {
      const def = buildFileMenu(SupportedLanguage.C);
      expect(def.label).toBe('File');
      const labels = def.items.map((i) => i.label);
      expect(labels.some((l) => /New File \(/.test(l))).toBe(true);
      expect(labels.some((l) => /New Header \(/.test(l))).toBe(true);
      expect(labels).toContain('Save');
      expect(labels).toContain('Save All');
      expect(labels).toContain('Open File...');
      expect(labels).toContain('Close File');
    });

    it('emits action ids prefixed with NEW_FILE_ACTION_PREFIX', () => {
      const def = buildFileMenu(SupportedLanguage.C);
      const newFile = def.items.find((i) => i.label.startsWith('New File'));
      expect(newFile?.action?.startsWith(NEW_FILE_ACTION_PREFIX)).toBe(true);
    });

    it('omits the New Header item when the language has no separate header extension', () => {
      const def = buildFileMenu(SupportedLanguage.JavaScript);
      const headerItem = def.items.find((i) => i.label.startsWith('New Header'));
      expect(headerItem).toBeUndefined();
    });

    it('inserts separators between groups', () => {
      const def = buildFileMenu(SupportedLanguage.C);
      const separators = def.items.filter((i) => i.separator === true);
      expect(separators.length).toBeGreaterThanOrEqual(2);
    });
  });

  describe('EDIT_MENU', () => {
    it('exposes the standard editing actions', () => {
      const labels = EDIT_MENU.items.map((i) => i.label);
      expect(labels).toEqual(
        expect.arrayContaining(['Undo', 'Redo', 'Cut', 'Copy', 'Paste', 'Select All']),
      );
    });

    it('uses Ctrl+Z for Undo and Ctrl+Y for Redo', () => {
      const undo = EDIT_MENU.items.find((i) => i.label === 'Undo');
      const redo = EDIT_MENU.items.find((i) => i.label === 'Redo');
      expect(undo?.shortcut).toBe('Ctrl+Z');
      expect(redo?.shortcut).toBe('Ctrl+Y');
    });
  });
});
