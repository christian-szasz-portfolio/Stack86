import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { EditorStorageService } from './editor-storage.service';
import { EditorDatabaseService } from './editor-database.service';
import { CompilerFile } from '../compiler/compiler.models';

class FakeDb {
  private readonly map = new Map<string, string>();
  public save = vi.fn(async (key: string, content: string) => {
    this.map.set(key, content);
  });
  public load = vi.fn(async (key: string): Promise<string | null> => {
    return this.map.has(key) ? (this.map.get(key) as string) : null;
  });
}

describe('EditorStorageService', () => {
  let service: EditorStorageService;
  let db: FakeDb;

  beforeEach(() => {
    db = new FakeDb();
    TestBed.configureTestingModule({
      providers: [
        EditorStorageService,
        { provide: EditorDatabaseService, useValue: db },
      ],
    });
    service = TestBed.inject(EditorStorageService);
  });

  afterEach(() => vi.restoreAllMocks());

  describe('compiler hydration flag', () => {
    it('starts not hydrated', () => {
      expect(service.isCompilerHydrated).toBe(false);
    });

    it('reflects markCompilerHydrated', () => {
      service.markCompilerHydrated();
      expect(service.isCompilerHydrated).toBe(true);
    });
  });

  describe('compiler project', () => {
    const files: CompilerFile[] = [
      { id: '1', name: 'main.c', content: 'int main(){}', isMain: true },
    ];

    it('saveCompilerProject writes JSON-serialized files under the compiler key', async () => {
      await service.saveCompilerProject(files);
      expect(db.save).toHaveBeenCalledWith('compiler-project', JSON.stringify(files));
    });

    it('loadCompilerProject parses stored JSON', async () => {
      await service.saveCompilerProject(files);
      const loaded = await service.loadCompilerProject();
      expect(loaded).toEqual(files);
    });

    it('returns null when nothing stored', async () => {
      expect(await service.loadCompilerProject()).toBeNull();
    });

    it('returns null when stored data is an empty array', async () => {
      await service.saveCompilerProject([]);
      expect(await service.loadCompilerProject()).toBeNull();
    });

    it('returns null when stored data is not an array', async () => {
      db.save.mockClear();
      await db.save('compiler-project', JSON.stringify({ not: 'an array' }));
      expect(await service.loadCompilerProject()).toBeNull();
    });
  });

  describe('assembler source', () => {
    it('round-trips assembler text', async () => {
      await service.saveAssemblerSource('MOV AX, 1');
      expect(await service.loadAssemblerSource()).toBe('MOV AX, 1');
      expect(db.save).toHaveBeenCalledWith('assembler-source', 'MOV AX, 1');
    });

    it('returns null when nothing stored', async () => {
      expect(await service.loadAssemblerSource()).toBeNull();
    });
  });
});
