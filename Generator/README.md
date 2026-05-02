# Esolang.Funge.Generator

Roslyn source generator that compiles [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) programs into C# partial methods at build time.

## Overview

Add `[GenerateFungeMethod]` to a `partial` method declaration.  
The generator reads the Funge-98 source (from a file or inline) and emits a complete C# implementation.

### Supported return types

| Return type | Description |
|---|---|
| `void` | Run to completion; discard output |
| `string` | Collect all output and return as a string |
| `Task` | Async run; discard output |
| `Task<string>` | Async run; return output string |
| `ValueTask` | Async run; discard output |
| `ValueTask<string>` | Async run; return output string |
| `IEnumerable<byte>` | Yield output bytes synchronously |
| `IAsyncEnumerable<byte>` | Yield output bytes asynchronously |

### Supported parameter types

| Parameter type | Role |
|---|---|
| `string` | Input fed to the program (`&` / `~`) |
| `System.IO.TextReader` | Input reader |
| `System.IO.Pipelines.PipeReader` | Input as pipe |
| `System.IO.TextWriter` | Output writer (`void` return only) |
| `System.IO.Pipelines.PipeWriter` | Output as pipe (`void` return only) |
| `CancellationToken` | Cancellation (async methods) |

## Installation

```
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

    // Async variant
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial Task<string> HelloWorldAsync(CancellationToken cancellationToken = default);

    // With explicit TextWriter output
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial void HelloWorldWriter(System.IO.TextWriter output);

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

## Diagnostics

| ID | Severity | Description |
|---|---|---|
| FG0001 | Error | `sourcePath` is empty and `InlineSource` is not set |
| FG0002 | Error | Unsupported return type |
| FG0003 | Error | Unsupported parameter type |
| FG0004 | Error | Source file not found in `AdditionalFiles` |
| FG0005 | Warning | C# language version is too low (requires ≥ C# 8) |
| FG0006 | Error | Duplicate input/output parameter |
| FG0007 | Error | Return type conflicts with explicit output parameter |
| FG0008 | Warning | Program uses output (`.`/`,`) but no output parameter or output return type is declared |
| FG0009 | Warning | Program uses input (`&`/`~`) but no input parameter is declared |
| FG0010 | Hidden | Input parameter declared but program never reads input |

## Target Frameworks

Generator: `netstandard2.0`  
Consumer projects: `net8.0` · `net9.0` · `net10.0`
