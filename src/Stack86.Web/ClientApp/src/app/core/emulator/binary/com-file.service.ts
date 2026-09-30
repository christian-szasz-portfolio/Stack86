/**
 * Service for importing and exporting .COM and .ASM files via browser File API.
 */

import { Service } from '@angular/core';

/** Result of importing a file. */
export interface FileImportResult<T> {
  readonly filename: string;
  readonly data: T;
}

@Service()
export class ComFileService {
  /** Download a .COM binary file. */
  public exportCom(bytes: Uint8Array, filename: string): void {
    const buffer = new ArrayBuffer(bytes.length);
    new Uint8Array(buffer).set(bytes);
    const blob = new Blob([buffer], { type: 'application/octet-stream' });
    ComFileService.triggerDownload(blob, filename);
  }

  /** Open a file picker for .COM files and return the raw bytes. */
  public importCom(): Promise<FileImportResult<Uint8Array>> {
    return new Promise((resolve, reject) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.com';

      input.addEventListener('change', () => {
        const file = input.files?.[0];
        if (!file) {
          reject(new Error('No file selected'));
          return;
        }

        file.arrayBuffer().then(
          (buffer) => resolve({ filename: file.name, data: new Uint8Array(buffer) }),
          (err) => reject(err),
        );
      });

      input.click();
    });
  }

  /** Download an .ASM text file. */
  public exportAsm(source: string, filename: string): void {
    const blob = new Blob([source], { type: 'text/plain' });
    ComFileService.triggerDownload(blob, filename);
  }

  /** Open a file picker for .ASM files and return the text content. */
  public importAsm(): Promise<FileImportResult<string>> {
    return new Promise((resolve, reject) => {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.asm';

      input.addEventListener('change', () => {
        const file = input.files?.[0];
        if (!file) {
          reject(new Error('No file selected'));
          return;
        }

        file.text().then(
          (text) => resolve({ filename: file.name, data: text }),
          (err) => reject(err),
        );
      });

      input.click();
    });
  }

  private static triggerDownload(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = filename;
    anchor.click();
    URL.revokeObjectURL(url);
  }
}
