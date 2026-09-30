import { Service } from '@angular/core';
import type * as Monaco from 'monaco-editor';

@Service()
export class MonacoEditorService {
  private monacoPromise: Promise<typeof Monaco> | null = null;

  public loadMonaco(): Promise<typeof Monaco> {
    if (!this.monacoPromise) {
      this.configureWorker();
      this.monacoPromise = import('monaco-editor').then((m) => {
        // Expose Monaco for e2e tests to drive the editor reliably across browsers.
        // Safe to expose: monaco-editor is a public library and exposes no app secrets.
        (globalThis as unknown as { __monaco__?: typeof Monaco }).__monaco__ = m;
        return m;
      });
    }
    return this.monacoPromise;
  }

  public dispose(editor: Monaco.editor.IStandaloneCodeEditor): void {
    editor.dispose();
  }

  private configureWorker(): void {
    self.MonacoEnvironment = {
      getWorker(_workerId: string, label: string): Worker {
        if (label === 'typescript' || label === 'javascript') {
          return new Worker(
            new URL('monaco-editor/esm/vs/language/typescript/ts.worker.js', import.meta.url),
            { type: 'module' },
          );
        }

        if (label === 'css' || label === 'scss' || label === 'less') {
          return new Worker(
            new URL('monaco-editor/esm/vs/language/css/css.worker.js', import.meta.url),
            { type: 'module' },
          );
        }

        if (label === 'json') {
          return new Worker(
            new URL('monaco-editor/esm/vs/language/json/json.worker.js', import.meta.url),
            { type: 'module' },
          );
        }

        if (label === 'html' || label === 'handlebars' || label === 'razor') {
          return new Worker(
            new URL('monaco-editor/esm/vs/language/html/html.worker.js', import.meta.url),
            { type: 'module' },
          );
        }

        return new Worker(
          new URL('monaco-editor/esm/vs/editor/editor.worker.js', import.meta.url),
          { type: 'module' },
        );
      },
    };
  }
}
