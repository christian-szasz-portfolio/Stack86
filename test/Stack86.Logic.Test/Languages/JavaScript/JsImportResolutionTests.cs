namespace Stack86.Logic.Test.Languages.JavaScript;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stack86.Logic.Languages;
using Stack86.Logic.Test.Compilation;

/// <summary>
/// End-to-end coverage for JavaScript intra-project ES module resolution: an
/// <c>import { add } from './helper.js'</c> whose relative specifier names another project file is
/// resolved by merging the imported file ahead of the importer (dependency order) and stripping
/// the import statement, <c>export</c> modifiers are removed so the merged source parses as a plain
/// script, and imports that do not resolve to a project file are reported as unavailable.
/// </summary>
[TestClass]
public sealed class JsImportResolutionTests
{
    [TestMethod]
    public async Task NamedImport_ResolvesProjectModule()
    {
        var files = new Dictionary<string, string>
        {
            ["helper.js"] = "export function add(a, b) { return a + b; }\n",
            ["main.js"] = "import { add } from './helper.js';\nadd(2, 3);\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        StringAssert.Contains(result.Value.Assembly!, ".CODE");
        Assert.IsEmpty(result.Value.Errors);
    }

    [TestMethod]
    public async Task Import_WithoutExtension_ResolvesProjectModule()
    {
        var files = new Dictionary<string, string>
        {
            ["helper.js"] = "export const FOUR = 4;\nexport function add(a, b) { return a + b; }\n",
            ["main.js"] = "import { add } from './helper';\nadd(FOUR, 5);\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        StringAssert.Contains(result.Value.Assembly!, ".CODE");
        Assert.IsEmpty(result.Value.Errors);
    }

    [TestMethod]
    public async Task Import_EmitsDependencyBeforeImporter()
    {
        // main.js is the entry but imports helper.js; helper must be emitted first
        // so that 'add' is defined before it is called.
        var files = new Dictionary<string, string>
        {
            ["main.js"] = "import { add } from './helper.js';\nadd(2, 3);\n",
            ["helper.js"] = "export function add(a, b) { return a + b; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        Assert.IsEmpty(result.Value.Errors);
    }

    [TestMethod]
    public async Task BareModuleImport_IsReportedAsUnavailable()
    {
        var files = new Dictionary<string, string>
        {
            ["main.js"] = "import { readFile } from 'fs';\nreadFile();\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value.Assembly);
        Assert.IsTrue(result.Value.Errors.Any(e => e.Message.Contains("fs") && e.Message.Contains("not available")));
    }

    [TestMethod]
    public async Task CircularImport_ResolvesWithoutInfiniteLoop()
    {
        // helper imports util and util imports helper: the dependency graph has a cycle.
        // The visited-set guard must break the cycle so merging terminates and both
        // modules' symbols end up in the single flat namespace.
        var files = new Dictionary<string, string>
        {
            ["main.js"] = "import { add } from './helper.js';\nadd(2, 3);\n",
            ["helper.js"] = "import { bump } from './util.js';\nexport function add(a, b) { return bump(a) + b; }\n",
            ["util.js"] = "import { add } from './helper.js';\nexport function bump(x) { return x + 1; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        Assert.IsEmpty(result.Value.Errors);
    }

    [TestMethod]
    public async Task DeepDependencyChain_EmitsInTransitiveOrder()
    {
        // main -> a -> b -> c: every module must be emitted after the module it imports
        // so each callee is defined before its caller in the flat namespace.
        var files = new Dictionary<string, string>
        {
            ["main.js"] = "import { fa } from './a.js';\nfa(1);\n",
            ["a.js"] = "import { fb } from './b.js';\nexport function fa(x) { return fb(x) + 1; }\n",
            ["b.js"] = "import { fc } from './c.js';\nexport function fb(x) { return fc(x) + 1; }\n",
            ["c.js"] = "export function fc(x) { return x + 1; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        Assert.IsEmpty(result.Value.Errors);
    }

    [TestMethod]
    public async Task DiamondDependency_EmitsSharedModuleOnce()
    {
        // main -> left, main -> right, left -> base, right -> base: the shared 'base'
        // module must be emitted exactly once (before both left and right) so its symbol
        // is not duplicated in the flat namespace.
        var files = new Dictionary<string, string>
        {
            ["main.js"] = "import { fl } from './left.js';\nimport { fr } from './right.js';\nfl(1) + fr(2);\n",
            ["left.js"] = "import { fb } from './base.js';\nexport function fl(x) { return fb(x) + 1; }\n",
            ["right.js"] = "import { fb } from './base.js';\nexport function fr(x) { return fb(x) + 2; }\n",
            ["base.js"] = "export function fb(x) { return x + 1; }\n",
        };

        var result = await CompileFixture.CompileFiles(SupportedLanguage.JavaScript, files);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Value.Assembly);
        Assert.IsEmpty(result.Value.Errors);
    }
}
