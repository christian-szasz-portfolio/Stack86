const ts = require('typescript');
const fs = require('fs');

const src = fs.readFileSync(process.argv[2], 'utf-8');
const r = ts.transpileModule(src, {
  compilerOptions: {
    target: ts.ScriptTarget.ES2015,
    module: ts.ModuleKind.None,
    strict: false,
    removeComments: true,
  },
});
console.log(r.outputText);
