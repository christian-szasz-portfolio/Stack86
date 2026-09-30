import { SupportedLanguage, getDefaultSourceExtension, getDefaultHeaderExtension } from '../../../../core/compiler/compiler.models';

export const NEW_FILE_ACTION_PREFIX = 'newFile:';

export interface MenuItem {
  label: string;
  icon?: string;
  shortcut?: string;
  action?: string;
  separator?: boolean;
  disabled?: boolean;
}

export interface MenuDefinition {
  label: string;
  items: MenuItem[];
}

export function buildFileMenu(language: SupportedLanguage): MenuDefinition {
  const sourceExt = getDefaultSourceExtension(language);
  const headerExt = getDefaultHeaderExtension(language);

  const items: MenuItem[] = [
    { label: `New File (${sourceExt})`, icon: 'file-code', action: `${NEW_FILE_ACTION_PREFIX}${sourceExt}` },
  ];
  if (headerExt) {
    items.push({ label: `New Header (${headerExt})`, icon: 'file-lines', action: `${NEW_FILE_ACTION_PREFIX}${headerExt}` });
  }
  items.push(
    { separator: true, label: '' },
    { label: 'Open File...', icon: 'folder-open', action: 'openFile', shortcut: 'Ctrl+O' },
    { separator: true, label: '' },
    { label: 'Save', icon: 'floppy-disk', action: 'save', shortcut: 'Ctrl+S' },
    { label: 'Save All', action: 'saveAll', shortcut: 'Ctrl+Shift+S' },
    { separator: true, label: '' },
    { label: 'Close File', icon: 'xmark', action: 'closeFile' },
  );

  return { label: 'File', items };
}

export const EDIT_MENU: MenuDefinition = {
  label: 'Edit',
  items: [
    { label: 'Undo', icon: 'rotate-left', action: 'undo', shortcut: 'Ctrl+Z' },
    { label: 'Redo', icon: 'rotate-right', action: 'redo', shortcut: 'Ctrl+Y' },
    { separator: true, label: '' },
    { label: 'Cut', icon: 'scissors', action: 'cut', shortcut: 'Ctrl+X' },
    { label: 'Copy', icon: 'copy', action: 'copy', shortcut: 'Ctrl+C' },
    { label: 'Paste', icon: 'paste', action: 'paste', shortcut: 'Ctrl+V' },
    { separator: true, label: '' },
    { label: 'Select All', icon: 'object-group', action: 'selectAll', shortcut: 'Ctrl+A' },
  ],
};
