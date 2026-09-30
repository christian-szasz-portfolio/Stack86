import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { ComFileService } from './com-file.service';

describe('ComFileService', () => {
  let service: ComFileService;
  let createdAnchors: HTMLAnchorElement[];

  beforeEach(() => {
    service = new ComFileService();
    createdAnchors = [];

    const originalCreateElement = document.createElement.bind(document);
    vi.spyOn(document, 'createElement').mockImplementation((tag: string) => {
      const el = originalCreateElement(tag) as HTMLElement;
      if (tag.toLowerCase() === 'a') {
        const anchor = el as HTMLAnchorElement;
        vi.spyOn(anchor, 'click').mockImplementation(() => undefined);
        createdAnchors.push(anchor);
      }
      return el;
    });

    if (typeof URL.createObjectURL !== 'function') {
      // jsdom may not implement these — patch as no-ops we can spy on.
      Object.defineProperty(URL, 'createObjectURL', { configurable: true, value: () => 'blob:test' });
      Object.defineProperty(URL, 'revokeObjectURL', { configurable: true, value: () => undefined });
    }

    vi.spyOn(URL, 'createObjectURL').mockReturnValue('blob:test');
    vi.spyOn(URL, 'revokeObjectURL').mockImplementation(() => undefined);
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('exportCom', () => {
    it('triggers a download with the correct filename', () => {
      const bytes = new Uint8Array([0x90, 0xCD, 0x21]);
      service.exportCom(bytes, 'hello.com');

      expect(URL.createObjectURL).toHaveBeenCalledTimes(1);
      const anchor = createdAnchors.at(-1);
      expect(anchor?.download).toBe('hello.com');
      expect(anchor?.href).toContain('blob:test');
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob:test');
    });
  });

  describe('exportAsm', () => {
    it('triggers a download with the asm content', () => {
      service.exportAsm('MOV AX, 1', 'prog.asm');
      const anchor = createdAnchors.at(-1);
      expect(anchor?.download).toBe('prog.asm');
      expect(URL.revokeObjectURL).toHaveBeenCalled();
    });
  });
});
