# Esolang.Funge.Generator

Roslyn source generator that compiles [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) programs into C# partial methods at build time.

## Overview

Add `[GenerateFungeMethod]` to a `partial` method declaration.  
The generator reads the Funge-98 source (from a file or inline) and emits a complete C# implementation.

### Supported return types

| Return type | Description |
| --- | --- |
| `void` | Run to completion; discard output |
| `int` | Return program exit code (`q` pops stack top, otherwise `0`) |
| `string` | Collect all output and return as a string |
| `Task` | Async run; discard output |
| `Task<int>` | Async run; return program exit code |
| `Task<string>` | Async run; return output string |
| `ValueTask` | Async run; discard output |
| `ValueTask<int>` | Async run; return program exit code |
| `ValueTask<string>` | Async run; return output string |
| `IEnumerable<byte>` | Yield output bytes synchronously |
| `IAsyncEnumerable<byte>` | Yield output bytes asynchronously |

### Supported parameter types

| Parameter type | Role |
| --- | --- |
| `string` | Input fed to the program (`&` / `~`) |
| `string[]` or `IEnumerable<string>` | Command-line arguments or environment variables. The generator binds these by both type and name heuristics: parameter names containing `arg` map to command-line arguments, and names containing `env` map to environment variables. Reported by `y`. |
| `System.IO.TextReader` | Input reader |
| `System.IO.Pipelines.PipeReader` | Input as pipe |
| `System.IO.TextWriter` | Explicit output sink for methods that do not return output text/bytes (including `void`, `int`, `Task`, `Task<int>`, `ValueTask`, `ValueTask<int>`) |
| `System.IO.Pipelines.PipeWriter` | Explicit pipe output sink for methods that do not return output text/bytes (including `void`, `int`, `Task`, `Task<int>`, `ValueTask`, `ValueTask<int>`) |
| `CancellationToken` | Cancellation (async methods) |

## Installation

```bash
dotnet add package Esolang.Funge.Generator
```

## Usage

### File-based

Add `.b98` files to your project using the `FungeSource` item group:

```xml
<ItemGroup>
  <FungeSource Include="Programs\*.b98" />
</ItemGroup>
```

Then declare partial methods:

```csharp
using Esolang.Funge;

partial class MyPrograms
{
    // Returns the program output as a string
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial string HelloWorld();

    // With custom arguments and environment variables (reported by 'y' instruction).
    // Parameter names matter here: names containing 'arg' bind as args, and names containing 'env' bind as envs.
    [GenerateFungeMethod("Programs/sysinfo.b98")]
    public static partial int RunWithArgs(string[] args, string[] envs);

    // Async variant
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial Task<string> HelloWorldAsync(CancellationToken cancellationToken = default);

    // With explicit TextWriter output
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial void HelloWorldWriter(System.IO.TextWriter output);

    // Exit code return can be combined with explicit output
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial Task<int> HelloWorldExitCodeAsync(System.IO.Pipelines.PipeWriter output, CancellationToken cancellationToken = default);

    // With string input
    [GenerateFungeMethod("Programs/echo.b98")]
    public static partial string Echo(string input);
}
```

### Inline source

Funge-98 code can be embedded directly as a string literal using `InlineSource`—no `.b98` file needed:

```csharp
[GenerateFungeMethod(InlineSource = "64+\"!dlroW ,olleH\">:#,_@")]
public static partial string HelloWorldInline();
```

## 3D (Trefunge) support

The generator fully supports 3D Funge-98 (Trefunge) programs.  
Within a source file, the form-feed character (`\f`, U+000C) separates Z-layers.

```.b98
# Layer Z=0 — jump into layer Z=1
l
```

### (form-feed character here separates layers)

```.b98
# Layer Z=1 — runs the Hello World program
>64+"!dlroW ,olleH">:#,_@
```

**3D direction instructions:**

| Instruction | Description |
| --- | --- |
| `h` | Set delta to High `(0, 0, −1)` |
| `l` | Set delta to Low `(0, 0, +1)` |
| `m` | Pop value; zero → Low, non-zero → High |

The generated runtime automatically handles XYZ coordinates, Z-axis wrapping, and 3D `g`/`p`/`x` operands.

## Logger Support

The generator automatically detects and injects logging support if an `ILogger` or `ILogger<T>` is available. It searches in the following order:

1. **Method Parameters**: If a parameter of type `ILogger` or `ILogger<T>` is declared, it is used directly.
2. **Primary Constructor Parameters**: If a primary constructor parameter of type `ILogger` or `ILogger<T>` is present in the class, it is used.
3. **Class Fields**: If no parameter or constructor argument is provided, the generator searches for a field of type `ILogger` or `ILogger<T>` within the containing class (including accessible fields in base classes).

**Example:**
```csharp
// Logger detected as primary constructor parameter
public partial class MyPrograms(ILogger<MyPrograms> logger)
{
    [GenerateFungeMethod("Programs/hello.b98")]
    public partial void HelloWorld(); // 'logger' parameter will be used automatically
}
```

## Diagnostics

| ID | Severity | Description |
| --- | --- | --- |
| [FG0001](Rules/FG0001.md) | Error | `sourcePath` is empty and `InlineSource` is not set |
| [FG0002](Rules/FG0002.md) | Error | Unsupported return type |
| [FG0003](Rules/FG0003.md) | Error | Unsupported parameter type |
| [FG0004](Rules/FG0004.md) | Error | Source file not found in `AdditionalFiles` |
| [FG0005](Rules/FG0005.md) | Warning | C# language version is too low (requires ≥ C# 8) |
| [FG0006](Rules/FG0006.md) | Error | Duplicate input/output parameter |
| [FG0007](Rules/FG0007.md) | Error | Return type conflicts with explicit output parameter |
| [FG0008](Rules/FG0008.md) | Info | Program appears to use output (`.`/`,`) but no output parameter or output return type is declared (static best-effort scan; runtime throws if reached) |
| [FG0009](Rules/FG0009.md) | Info | Program appears to use input (`&`/`~`) but no input parameter is declared (static best-effort scan; runtime throws if reached) |
| [FG0010](Rules/FG0010.md) | Hidden | Input parameter declared but program never reads input |
| [FG0011](Rules/FG0011.md) | Error | Method must be partial |

## Funge-98 Compliance

The generated runtime (`FungeRuntime`) implements Funge-98 core execution including Trefunge 3D navigation (`h` / `l` / `m`),
concurrency (`t`), and stack stack operations (`{` / `}` / `u`), while still excluding fingerprints.

| Category | Instructions | Status |
| --- | --- | --- |
| Stack | `0`–`9` `a`–`f` `:` `$` `\` `n` | ✅ |
| Arithmetic | `+` `-` `*` `/` `%` | ✅ |
| Comparison | `` ` `` `!` | ✅ |
| Direction | `>` `<` `^` `v` `h` `l` `?` `[` `]` `r` `x` `m` `w` | ✅ |
| Branching | `_` `\|` | ✅ |
| Movement | `#` `;` `j` | ✅ |
| String / char | `"` `'` `s` | ✅ (stringmode contiguous spaces are SGML-style) |
| Storage (self-modifying) | `g` `p` (with storage offset) | ✅ |
| I/O | `.` `,` `&` `~` | ✅ |
| Misc | `z` `@` | ✅ |
| Exit code | `q` | ✅ pops stack top and returns it as method exit code (`@` returns `0`) |
| Iteration | `k` | ✅ |
| Concurrency | `t` | ✅ |
| Stack stack | `{` `}` `u` | ✅ |
| System info | `y` | ✅ |
| File I/O | `i` `o` | ✅ |
| System exec | `=` | ✅ |
| Fingerprint load/unload | `(` `)` | ⚠️ Stub (see below) |
| Fingerprint instructions | `A`–`Z` | ❌ Always reflects |
| 3D (Trefunge) | `h` `l` `m` | ✅ |
| ND-generalized space | dimensions > 3 | ❌ not implemented |

### Fingerprint support detail

| Instruction | Status | Behaviour |
| --- | --- | --- |
| `(` Load Fingerprint | ⚠️ Stub | Pops count `n`, discards `n` chars; pushes fingerprint ID `0` and success flag `1`. No dispatch table is registered. |
| `)` Unload Fingerprint | ⚠️ Stub | Pops count `n`, discards `n` chars; pushes fingerprint ID `0` and success flag `1`. No dispatch table is modified. |
| `A`–`Z` Fingerprint Instructions | ❌ Reflects | Always reverses delta. No fingerprint dispatch table is maintained. |

## References

- [Funge-98 Specification](https://codeberg.org/catseye/Funge-98/src/branch/master/doc/funge98.markdown) — Chris Pressey, Cat's Eye Technologies
- [Funge-98 — Esolangs Wiki](https://esolangs.org/wiki/Funge-98)
- [Mycology — Funge-98 compliance test suite](https://github.com/Deewiant/Mycology)

## Target Frameworks

Generator: `netstandard2.0`  
Consumer projects: `net8.0` · `net9.0` · `net10.0`
