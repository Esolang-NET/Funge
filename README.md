# Esolang.Funge

[![.NET](https://github.com/Esolang-NET/Funge/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Esolang-NET/Funge/actions/workflows/dotnet.yml)

## Quick Start (Generator)

Write Funge-98 once, call it as a C# method.

```csharp
using Esolang.Funge;

Console.WriteLine(FungeSample.HelloWorld());

partial class FungeSample
{
    // File-based
    [GenerateFungeMethod("Programs/hello.b98")]
    public static partial string HelloWorld();

    // Or inline — no .b98 file needed
    [GenerateFungeMethod(InlineSource = "64+\"!dlroW ,olleH\">:#,_@")]
    public static partial string HelloWorldInline();
}

// output:
// Hello, World!
// Hello, World!
```

## Generator Guide

For detailed Generator signatures and patterns (`string`, `TextReader`, `PipeReader`, `TextWriter`, `PipeWriter`, sync/async returns, byte-sequence returns, inline source), see:

- [Generator README](./Generator/README.md)

For runnable examples covering all return types and inline source, see:

- [UseConsole sample](./samples/Generator.UseConsole/README.md)

## Funge-98 Support Status

Current implementation status across packages:

| Area | Status |
|---|---|
| Core Funge-98 instructions | ✅ Implemented |
| Trefunge 3D navigation (`h` `l` `m`) | ✅ Implemented |
| Coordinates and storage space | ✅ 3D (`X`,`Y`,`Z`) |
| Fingerprints (`(` `)` / `A`-`Z`) | ❌ Not implemented (reflect) |
| File I/O (`i` `o`) | ✅ Implemented |
| System exec (`=`) | ✅ Implemented |

Details:

- Parser behavior and space model: [Parser README](./Parser/README.md)
- Runtime execution and instruction compliance: [Processor README](./Processor/README.md)
- Generated runtime subset and limitations: [Generator README](./Generator/README.md)
- CLI behavior and scope: [Interpreter README](./Interpreter/README.md)

## Install

```bash
dotnet add package Esolang.Funge.Generator
dotnet add package Esolang.Funge.Parser
dotnet add package Esolang.Funge.Processor
dotnet tool install -g dotnet-funge --prerelease
```

## Choose Package

| Want to do | Package |
|---|---|
| Generate C# methods from Funge-98 at compile time | Esolang.Funge.Generator |
| Parse source into a `FungeSpace` | Esolang.Funge.Parser |
| Execute Funge-98 in-process | Esolang.Funge.Processor |
| Run Funge-98 from CLI | dotnet-funge |

## NuGet

| Project | NuGet | Summary |
|---|---|---|
| [dotnet-funge](./Interpreter/README.md) | [![NuGet: dotnet-funge](https://img.shields.io/nuget/v/dotnet-funge?logo=nuget)](https://www.nuget.org/packages/dotnet-funge/) | Funge-98 command-line interpreter. |
| [Esolang.Funge.Generator](./Generator/README.md) | [![NuGet: Esolang.Funge.Generator](https://img.shields.io/nuget/v/Esolang.Funge.Generator?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Generator/) | Funge-98 source generator. |
| [Esolang.Funge.Parser](./Parser/README.md) | [![NuGet: Esolang.Funge.Parser](https://img.shields.io/nuget/v/Esolang.Funge.Parser?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Parser/) | Funge-98 source parser. |
| [Esolang.Funge.Processor](./Processor/README.md) | [![NuGet: Esolang.Funge.Processor](https://img.shields.io/nuget/v/Esolang.Funge.Processor?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Processor/) | Funge-98 execution engine. |

## Framework Support

| Project | Target frameworks |
|---|---|
| Esolang.Funge.Generator | netstandard2.0 |
| Esolang.Funge.Parser | net8.0, net9.0, net10.0, netstandard2.0 |
| Esolang.Funge.Processor | net8.0, net9.0, net10.0 |
| dotnet-funge | net8.0, net9.0, net10.0 |

## Changelog

- [CHANGELOG](./CHANGELOG.md)

## See also

- [Funge-98 specification](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown)
- [Befunge-93 / Befunge-98 on Esolangs wiki](https://esolangs.org/wiki/Befunge)
