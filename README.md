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

For detailed Generator signatures and patterns, see:

- [Generator README](./Generator/README.md)

### Generator Signatures

| Attribute Argument | `partial` Method Parameters (Input) | `partial` Method Return Types (Output) |
| :--- | :--- | :--- |
| `string` (Path/Inline) | `TextReader?`, `PipeReader?`, `byte[]?` | `void`, `string`, `string?`, `int`, `Task`, `ValueTask`, `IEnumerable<byte>`, `IAsyncEnumerable<byte>` |

For runnable examples, see:

- [UseConsole sample](./samples/Generator.UseConsole/README.md)

## Funge-98 Support Status

| Area | Status |
|---|---|
| Core Funge-98 instructions | ✅ |
| 3D navigation (`h` `l` `m`) | ✅ |
| Coordinates and storage space | ✅ 3D (`X`,`Y`,`Z`) |
| Fingerprint load/unload (`(` `)`) | ✅ Full dispatch (requires fingerprint registration) |
| Fingerprint instructions (`A`–`Z`) | ✅ Dispatched when fingerprint loaded; reflects otherwise |
| File I/O (`i` `o`) | ✅ |
| System exec (`=`) | ✅ |
| System info (`y`) | ✅ |

Details:

- Parser behavior and space model: [Parser README](./Parser/README.md)
- Runtime execution and instruction compliance: [Processor README](./Processor/README.md)
- Generated runtime subset and limitations: [Generator README](./Generator/README.md)
- CLI behavior and scope: [Interpreter README](./Interpreter/README.md)

## Fingerprint Support

Built-in fingerprints shipped with this repository:

| Fingerprint | Handprint | Package | Status |
|---|---|---|---|
| [`NULL`](./Abstractions/README.md) | `0x4E554C4C` | [Esolang.Funge.Abstractions](./Abstractions/README.md) | ✅ All 26 instructions reflect |
| [`FILE`](./Fingerprints.File/README.md) | `0x46494C45` | [Esolang.Funge.Fingerprints.File](./Fingerprints.File/README.md) | ✅ C D G M O P R S W |

To register fingerprints with the Generator, use `[FingerprintsProvider]` — see [Generator README](./Generator/README.md).

## Install

```bash
dotnet add package Esolang.Funge.Generator
dotnet add package Esolang.Funge.Parser
dotnet add package Esolang.Funge.Processor
dotnet tool install -g dotnet-funge
```

## Choose Package

| Want to do | Package |
|---|---|
| Generate C# methods from Funge-98 at compile time | Esolang.Funge.Generator |
| Parse source into a `FungeSpace` | Esolang.Funge.Parser |
| Execute Funge-98 in-process | Esolang.Funge.Processor |
| Implement or use built-in fingerprints | Esolang.Funge.Abstractions |
| Use FILE fingerprint (file I/O) | Esolang.Funge.Fingerprints.File |
| Run Funge-98 from CLI | dotnet-funge |

## NuGet

| Project | NuGet | Summary |
|---|---|---|
| [dotnet-funge](./Interpreter/README.md) | [![NuGet: dotnet-funge](https://img.shields.io/nuget/v/dotnet-funge?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/dotnet-funge/) | Funge-98 command-line interpreter. |
| [Esolang.Funge.Generator](./Generator/README.md) | [![NuGet: Esolang.Funge.Generator](https://img.shields.io/nuget/v/Esolang.Funge.Generator?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Generator/) | Funge-98 source generator. |
| [Esolang.Funge.Parser](./Parser/README.md) | [![NuGet: Esolang.Funge.Parser](https://img.shields.io/nuget/v/Esolang.Funge.Parser?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Parser/) | Funge-98 source parser. |
| [Esolang.Funge.Processor](./Processor/README.md) | [![NuGet: Esolang.Funge.Processor](https://img.shields.io/nuget/v/Esolang.Funge.Processor?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Processor/) | Funge-98 execution engine. |
| [Esolang.Funge.Abstractions](./Abstractions/README.md) | [![NuGet: Esolang.Funge.Abstractions](https://img.shields.io/nuget/v/Esolang.Funge.Abstractions?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Abstractions/) | Fingerprint abstractions and built-in fingerprints. |
| [Esolang.Funge.Fingerprints.File](./Fingerprints.File/README.md) | [![NuGet: Esolang.Funge.Fingerprints.File](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.File?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.File/) | FILE fingerprint (file I/O). |

## Framework Support

| Project | Target frameworks |
|---|---|
| Esolang.Funge.Generator | netstandard2.0 |
| Esolang.Funge.Parser | net8.0, net9.0, net10.0, netstandard2.0 |
| Esolang.Funge.Processor | net8.0, net9.0, net10.0 |
| Esolang.Funge.Abstractions | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.File | netstandard2.0, netstandard2.1, net10.0 |
| dotnet-funge | net8.0, net9.0, net10.0 |

## Changelog

- [CHANGELOG](./CHANGELOG.md)

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## See also

- [Funge-98 specification](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown)
- [Befunge-93 / Befunge-98 on Esolangs wiki](https://esolangs.org/wiki/Befunge)
