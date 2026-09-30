// TypeScript → JavaScript transpiler script for Stack86.
// Called by NodeTypeScriptTranspiler.cs as:
//   node transpile.cjs <input.ts>
// Writes transpiled JavaScript to stdout; errors to stderr.
// Requires the `typescript` package, resolved via NODE_PATH env var.

'use strict';

const fs = require('fs');
const path = require('path');

const inputFile = process.argv[2];
if (!inputFile) {
  process.stderr.write('Usage: node transpile.cjs <input.ts>\n');
  process.exit(1);
}

let ts;
try {
  ts = require('typescript');
} catch {
  process.stderr.write('error: Cannot find module "typescript". Ensure NODE_PATH includes the correct node_modules directory.\n');
  process.exit(1);
}

let source;
try {
  source = fs.readFileSync(path.resolve(inputFile), 'utf-8');
} catch (err) {
  process.stderr.write(`error: Cannot read file "${inputFile}": ${err.message}\n`);
  process.exit(1);
}

const result = ts.transpileModule(source, {
  compilerOptions: {
    target: ts.ScriptTarget.ES2015,
    module: ts.ModuleKind.None,
    strict: false,
    removeComments: true,
    esModuleInterop: false,
    skipLibCheck: true,
    noEmit: false,
  },
  reportDiagnostics: true,
});

if (result.diagnostics && result.diagnostics.length > 0) {
  for (const diag of result.diagnostics) {
    const msg = ts.flattenDiagnosticMessageText(diag.messageText, '\n');
    const line = diag.file && diag.start !== undefined
      ? diag.file.getLineAndCharacterOfPosition(diag.start).line + 1
      : 0;
    const severity = diag.category === ts.DiagnosticCategory.Error ? 'error' : 'warning';
    process.stderr.write(`:${line}: ${severity}: ${msg}\n`);
  }
}

process.stdout.write(result.outputText);
