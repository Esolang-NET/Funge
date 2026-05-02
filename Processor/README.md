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
| Direction | `>` `<` `^` `v` `?` `[` `]` `r` `x` |
| Movement | `#` (trampoline), `;` (jump over), `j` (jump forward) |
| String mode | `"` |
| Branching | `_` `|` `w` |
| I/O | `.` `,` `&` `~` |
| Storage | `p` (put), `g` (get) |
| Reflection | `k` (iterate) |
| Concurrency | `t` (split IP) |
| Stack stack | `{` `}` `u` |
| Misc | `z` (no-op), `q` (quit) |
| Reflected | `(` `)` `i` `o` `h` `l` `m` (fingerprints / 3-D / file I/O not implemented) |

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

## Target Frameworks

`net8.0` · `net9.0` · `net10.0`

AOT / trimming compatible.
