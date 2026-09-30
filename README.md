# Stack86 (demo)

Stack86 is an 8086 emulator, an assembler IDE and a compiler that turns C, C++, C#, JavaScript and
TypeScript into 8086 assembly, all in the browser, on Angular 22 and ASP.NET Core (.NET 10). This
repository is the source of its public demo: the compiler, the emulator and the IDE as they are in
the full product, without accounts or a database, so anyone can try it at the address below and
write code straight away.

- **Live demo:** https://stack86.christianszasz.dev (once it is deployed)
- **Case study:** https://christianszasz.dev/work/stack86

## What a visitor gets

You open the compiler, pick a sample (arithmetic in C, a class hierarchy in C++, an A* search
split over three files) or write your own, and compile it. The build log streams in as each
stage runs, problems land in their own tab with a line and column, and the assembly appears beside
the source. One click sends it to the emulator, where you can step through it an instruction at a
time, set breakpoints in the gutter and watch the registers, flags, memory and stack change.

The emulator also stands on its own: write 8086 assembly directly, or start from one of ten
assembly samples.

## How it differs from the full product

| Area | Full product | This demo |
| --- | --- | --- |
| Source languages | C, C++, C#, Go, Java, JavaScript, Python, Rust, TypeScript | C, C++, C#, JavaScript, TypeScript |
| Accounts | Registration, sign-in, email verification, password reset, two-factor, trials | None; no sign-in |
| Persistence | EF Core with an Identity schema | None; nothing is stored |

Everything else, the pipeline, the intermediate representation, the code generation and the
emulator, is the same code as the full product.

## The compiler

A compile runs a pipeline of stages, and each language declares its own chain of them as data in
`LanguageSpecs`:

```text
Source > Preprocess > ValidateExternal > ValidateCapabilities > Transpile > LowerToIr > OptimizeIr > ValidateIr > EmitAssembly > 8086 assembly
```

| Language | How it reaches the intermediate representation |
| --- | --- |
| C | Its own frontend, after `#include` merging and a syntax check by TCC |
| C++ | Transpiled to C, then checked by TCC and lowered as C |
| C# | Its own frontend, parsed with Roslyn |
| JavaScript | Its own frontend, after a syntax check with `node --check` |
| TypeScript | Transpiled to JavaScript, then lowered as JavaScript |

```csharp
await CompilationPipeline
    .For(registry, irValidator, language, files)
    .UseDefaultsFor(language)
    .RunAsync(cancellation);
```

`CompilerProvider` builds and runs that pipeline. Streaming uses `RunStreamingAsync`, which
emits log lines, heartbeats and the final result as NDJSON. The stages that call external tools
run under Polly timeouts and circuit breakers, and a missing tool turns into a warning rather than
a failed build.

C is the most complete frontend:

| Category | Supported |
| --- | --- |
| Types | `int`, `char`, `void`, pointers, arrays, structs, unions, enums, typedefs, function pointers |
| Statements | `if`/`else`, `while`, `do`/`while`, `for`, `switch`/`case`/`default`, `break`, `continue`, `return` |
| Operators | Arithmetic, bitwise, comparison, logical, assignment and compound assignment, unary, ternary, cast, `sizeof`, comma |
| Expressions | Function calls, member access (`.` and `->`), array subscripts, postfix `++` and `--` |
| Input and output | `printf` with `%d`, `%c` and `%s`, compiled to `INT 21h` calls |
| Standard library | `strlen`, `strcpy`, `strcmp`, `memset`, `memcpy`, `rand`, `atoi` and more, served by the emulator through `INT 86h` |
| Preprocessor | `#include` with recursive multi-file merging, `#define` |

## The emulator

It runs entirely in the browser.

| Category | Instructions |
| --- | --- |
| Data movement | `MOV`, `PUSH`, `POP`, `XCHG`, `LEA` |
| Arithmetic | `ADD`, `SUB`, `CMP`, `INC`, `DEC`, `MUL`, `DIV`, `NEG` |
| Logic | `AND`, `OR`, `XOR`, `NOT`, `SHL`/`SAL`, `SHR` |
| Control flow | `JMP`, `JE`/`JZ`, `JNE`/`JNZ`, `JG`, `JGE`, `JL`, `JLE`, `JA`, `JB`, `JC`, `CALL`, `RET`, `LOOP` |
| Other | `NOP`, `HLT`, `INT` |
| Directives | `.MODEL`, `.STACK`, `.CODE`, `DB`, `DW`, `STRUC`/`ENDS`, `PROC`/`ENDP` |

`INT 21h` covers character input, character output, string output and exit (`AH` = `01h`, `02h`,
`09h`, `4Ch`). `INT 86h` is my own: 42 services that stand in for the C standard library, so
compiled C can call `strlen` or `rand` without the emulator having to run a libc.

The debugger steps, runs (all at once or animated at a speed you choose), pauses and resets, and
stops at breakpoints set in the editor's gutter.

## How it is built

| Path | What it holds |
| --- | --- |
| `src/Stack86.Web` | The ASP.NET Core host: serves the built client, the security middleware, the compile queue and the health probes |
| `src/Stack86.Web/ClientApp` | The Angular 22 client: Monaco editors, NgRx store with effects and facades, signal-based selectors, built with Vite through Analog |
| `src/Stack86.Api` | The compiler controller and its request and response models |
| `src/Stack86.Logic` | The pipeline, the language frontends and transpilers, the IR, its optimiser and the code generator |
| `src/Stack86.Common` | Cross-cutting pieces: exceptions, JSON options, dependency injection, security options, a clock and request validation |
| `test/` | MSTest projects for the API and the logic, and integration tests |

Compiling is the expensive part of a public demo, so the host guards it:

- Compiles go through a bounded queue: four at a time, eight waiting, and a request beyond that is
  turned away rather than left to pile up.
- Compiling has its own rate limit of 20 a minute per caller, apart from the general limit.
  Forwarded headers are read first, so behind a reverse proxy the limit applies to each visitor
  rather than to the proxy.
- A request body is capped at four times the source limit before it is parsed, since the source
  limit itself can only be checked after parsing.
- A Content Security Policy with a nonce per request, and the usual security headers.
- `/health` for liveness and `/health/ready` for readiness. Neither touches the compiler.
- Log capture into Azure Table Storage for a daily digest email, when a storage connection is
  configured.

## Tests and checks

StyleCop and the .NET analyzers run in every build, and the build is kept at zero warnings. The
client adds ESLint, Vitest unit tests, and Playwright tests that run against a production build.

## Hosting

The `Dockerfile` at the root builds the client, publishes the host and adds the tools the compiler
calls: TCC for C and a pinned Node.js for JavaScript and the TypeScript transpiler. The image
serves plain HTTP behind a proxy that terminates TLS. The compile queue lives in memory and is
safe to lose, so it can scale to zero. Its one optional secret, the storage connection for the
daily digest, comes from the environment and is never written in a committed file.

## Known limitations

- **It does not build from a clone.** The code depends on private packages from my Common library
  (log capture and the daily digest email), which live on a private package feed. Without access
  to that feed the restore fails, so a clone is for reading the code. The live demo is the way to
  try it.
- **One instance.** The queue and the rate limits are per process, so a second instance would
  double both.
- **Programs are small by design.** The source is capped at 256 KB, and the emulator implements
  the subset of the 8086 listed above rather than the whole instruction set.

## Licence

All rights reserved. You may read, clone and run the code to evaluate my work; any other use needs
my permission. See [LICENSE](LICENSE).

---

*This project has been co-authored by Claude Code.*
