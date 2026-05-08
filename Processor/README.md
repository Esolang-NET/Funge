# Esolang.Funge.Processor

Execution engine for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) programs.

## Overview

`FungeProcessor` executes a `FungeSpace` loaded by `Esolang.Funge.Parser`.  
It implements the full Funge-98 core instruction set including concurrent Instruction Pointers.

### Supported instructions

| Category | Instructions |
|---|---|
| Stack | `0`–`9` `a`–`f` (push), `:` (dup), `$` (pop), `\` (swap), `n` (clear) |
| Arithmetic | `+` `-` `*` `/` `%` |
| Comparison | `` ` `` `!` |
| Direction | `>` `<` `^` `v` `h` `l` `?` `[` `]` `r` `x` `m` |
| Movement | `#` (trampoline), `;` (jump over), `j` (jump forward) |
| String mode | `"` |
| Branching | `_` `|` `w` |
| I/O | `.` `,` `&` `~` |
| Storage | `p` (put), `g` (get) |
| Reflection | `k` (iterate) |
| Concurrency | `t` (split IP) |
| Stack stack | `{` `}` `u` |
| Misc | `z` (no-op), `q` (quit) |
| Reflected | `(` `)` (fingerprints not implemented) |

## Funge-98 Compliance

Targets **Funge-98** with 3D navigation (`h`/`l`/`m`). Fingerprint extensions are intentionally out of scope.

| Category | Instructions | Status |
|---|---|---|
| Stack | `0`–`9` `a`–`f` `:` `$` `\` `n` | ✅ |
| Arithmetic | `+` `-` `*` `/` `%` | ✅ |
| Comparison | `` ` `` `!` | ✅ |
| Direction (cardinal) | `>` `<` `^` `v` `?` | ✅ |
| Direction (Funge-98) | `[` `]` `r` `x` `w` | ✅ |
| Branching | `_` `\|` | ✅ |
| Movement | `#` `;` `j` | ✅ |
| Iteration | `k` | ✅ |
| String / char | `"` `'` `s` | ✅ |
| Storage (self-modifying) | `g` `p` (with storage offset) | ✅ |
| I/O | `.` `,` `&` `~` | ✅ |
| Concurrency | `t` | ✅ |
| Stack stack | `{` `}` `u` | ✅ |
| System info | `y` | ✅ |
| Misc | `z` `@` `q` | ✅ |
| File I/O | `i` `o` | ✅ |
| System exec | `=` | ✅ |
| Fingerprints | `(` `)` `A`–`Z` | ❌ reflects (not implemented) |
| 3D (Trefunge) | `h` `l` `m` | ✅ |

## Installation

```
dotnet add package Esolang.Funge.Processor
```

## Usage

```csharp
using Esolang.Funge.Parser;
using Esolang.Funge.Processor;

var space = FungeParser.ParseFile("hello.b98");
var proc = new FungeProcessor(space, Console.Out, Console.In);
int exitCode = proc.Run();
```

`FungeProcessor` accepts optional `TextWriter` (output) and `TextReader` (input) arguments, defaulting to `Console.Out` / `Console.In`.  
`Run()` accepts an optional `CancellationToken` and returns the exit code set by `q` (0 if not used).

## References

- [Funge-98 Specification](https://codeberg.org/catseye/Funge-98/src/branch/master/doc/funge98.markdown) — Chris Pressey, Cat's Eye Technologies
- [Funge-98 — Esolangs Wiki](https://esolangs.org/wiki/Funge-98)
- [Mycology — Funge-98 compliance test suite](https://github.com/Deewiant/Mycology)

## Target Frameworks

`net8.0` · `net9.0` · `net10.0`

AOT / trimming compatible.
