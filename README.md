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
| [`ARRY`](./Fingerprints.Arry/README.md) | `0x41525259` | [Esolang.Funge.Fingerprints.Arry](./Fingerprints.Arry/README.md) | ✅ A B C D E F G |
| [`BASE`](./Fingerprints.Base/README.md) | `0x42415345` | [Esolang.Funge.Fingerprints.Base](./Fingerprints.Base/README.md) | ✅ B H I N O |
| [`BOOL`](./Fingerprints.Bool/README.md) | `0x424F4F4C` | [Esolang.Funge.Fingerprints.Bool](./Fingerprints.Bool/README.md) | ✅ A N O X |
| [`DATE`](./Fingerprints.Date/README.md) | `0x44415445` | [Esolang.Funge.Fingerprints.Date](./Fingerprints.Date/README.md) | ✅ A C D J T W Y |
| [`FILE`](./Fingerprints.File/README.md) | `0x46494C45` | [Esolang.Funge.Fingerprints.File](./Fingerprints.File/README.md) | ✅ C D G L O P R S W |
| [`FIXP`](./Fingerprints.Fixp/README.md) | `0x46495850` | [Esolang.Funge.Fingerprints.Fixp](./Fingerprints.Fixp/README.md) | ✅ A B C D I J N O P Q R S T U V X |
| [`FRTH`](./Fingerprints.Frth/README.md) | `0x46525448` | [Esolang.Funge.Fingerprints.Frth](./Fingerprints.Frth/README.md) | ✅ D L O P R |
| [`IMTH`](./Fingerprints.Imth/README.md) | `0x494D5448` | [Esolang.Funge.Fingerprints.Imth](./Fingerprints.Imth/README.md) | ✅ A B C D E F G H I L N R S T U X Z |
| [`INDV`](./Fingerprints.Indv/README.md) | `0x494E4456` | [Esolang.Funge.Fingerprints.Indv](./Fingerprints.Indv/README.md) | ✅ G P V W |
| [`LONG`](./Fingerprints.Long/README.md) | `0x4C4F4E47` | [Esolang.Funge.Fingerprints.Long](./Fingerprints.Long/README.md) | ✅ A B D E L M N O P R S Z |
| `MODU` | `0x4D4F4455` | [Esolang.Funge.Fingerprints.Modu](./Fingerprints.Modu/README.md) | ✅ M R U |
| [`RAND`](./Fingerprints.Rand/README.md) | `0x52414E44` | [Esolang.Funge.Fingerprints.Rand](./Fingerprints.Rand/README.md) | ✅ I M R S T |
| `ROMA` | `0x524F4D41` | [Esolang.Funge.Fingerprints.Roma](./Fingerprints.Roma/README.md) | ✅ C D I L M V X |
| [`STCK`](./Fingerprints.Stck/README.md) | `0x5354434B` | [Esolang.Funge.Fingerprints.Stck](./Fingerprints.Stck/README.md) | ✅ B C D G K N P R S T U W Z |
| [`STRN`](./Fingerprints.Strn/README.md) | `0x5354524E` | [Esolang.Funge.Fingerprints.Strn](./Fingerprints.Strn/README.md) | ✅ A C D F G I L M N P R S V |
| [`TIME`](./Fingerprints.Time/README.md) | `0x54494D45` | [Esolang.Funge.Fingerprints.Time](./Fingerprints.Time/README.md) | ✅ D F G H L M O S W Y |
| [`3DSP`](./Fingerprints.ThreeDsp/README.md) | `0x33445350` | [Esolang.Funge.Fingerprints.ThreeDsp](./Fingerprints.ThreeDsp/README.md) | ✅ A B C D L M N P R S T U V X Y Z |
| [`CPLI`](./Fingerprints.Cpli/README.md) | `0x43504C49` | [Esolang.Funge.Fingerprints.Cpli](./Fingerprints.Cpli/README.md) | ✅ A D M O S V |
| [`CSHP`](./Fingerprints.Cshp/README.md) | `0x43534850` | [Esolang.Funge.Fingerprints.Cshp](./Fingerprints.Cshp/README.md) | ✅ E I S |
| [`DIRF`](./Fingerprints.Dirf/README.md) | `0x44495246` | [Esolang.Funge.Fingerprints.Dirf](./Fingerprints.Dirf/README.md) | ✅ C M R |
| [`EVAR`](./Fingerprints.Evar/README.md) | `0x45564152` | [Esolang.Funge.Fingerprints.Evar](./Fingerprints.Evar/README.md) | ✅ G N P V |
| [`FPDP`](./Fingerprints.Fpdp/README.md) | `0x46504450` | [Esolang.Funge.Fingerprints.Fpdp](./Fingerprints.Fpdp/README.md) | ✅ A B C D E F G H I K L M N P Q S T V X Y |
| [`FPRT`](./Fingerprints.Fprt/README.md) | `0x46505254` | [Esolang.Funge.Fingerprints.Fprt](./Fingerprints.Fprt/README.md) | ✅ D F I L S |
| [`FPSP`](./Fingerprints.Fpsp/README.md) | `0x46505350` | [Esolang.Funge.Fingerprints.Fpsp](./Fingerprints.Fpsp/README.md) | ✅ A B C D E F G H I K L M N P Q S T V X Y |
| [`HRTI`](./Fingerprints.Hrti/README.md) | `0x48525449` | [Esolang.Funge.Fingerprints.Hrti](./Fingerprints.Hrti/README.md) | ✅ E G M S T |
| [`ICAL`](./Fingerprints.Ical/README.md) | `0x4943414C` | [Esolang.Funge.Fingerprints.Ical](./Fingerprints.Ical/README.md) | ✅ A F I N O R S X |
| [`JSTR`](./Fingerprints.Jstr/README.md) | `0x4A535452` | [Esolang.Funge.Fingerprints.Jstr](./Fingerprints.Jstr/README.md) | ✅ G P |
| [`ORTH`](./Fingerprints.Orth/README.md) | `0x4F525448` | [Esolang.Funge.Fingerprints.Orth](./Fingerprints.Orth/README.md) | ✅ A E G O P S V W X Y Z |
| [`REFC`](./Fingerprints.Refc/README.md) | `0x52454643` | [Esolang.Funge.Fingerprints.Refc](./Fingerprints.Refc/README.md) | ✅ D R |
| [`SETS`](./Fingerprints.Sets/README.md) | `0x53455453` | [Esolang.Funge.Fingerprints.Sets](./Fingerprints.Sets/README.md) | ✅ A C D G I M P R S U W X Z |
| [`TERM`](./Fingerprints.Term/README.md) | `0x5445524D` | [Esolang.Funge.Fingerprints.Term](./Fingerprints.Term/README.md) | ✅ C D G H L S U |
| [`TOYS`](./Fingerprints.Toys/README.md) | `0x544F5953` | [Esolang.Funge.Fingerprints.Toys](./Fingerprints.Toys/README.md) | ✅ A B C D E F G H I J K L M N O P Q R S T U V W X Y Z |

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
| Use ARRY fingerprint (array-like space access) | Esolang.Funge.Fingerprints.Arry |
| Use BASE fingerprint (base conversion I/O) | Esolang.Funge.Fingerprints.Base |
| Use BOOL fingerprint (Boolean logic) | Esolang.Funge.Fingerprints.Bool |
| Use DATE fingerprint (calendar arithmetic) | Esolang.Funge.Fingerprints.Date |
| Use FILE fingerprint (file I/O) | Esolang.Funge.Fingerprints.File |
| Use FIXP fingerprint (fixed-point math) | Esolang.Funge.Fingerprints.Fixp |
| Use FRTH fingerprint (Forth-style stack helpers) | Esolang.Funge.Fingerprints.Frth |
| Use IMTH fingerprint (integer math helpers) | Esolang.Funge.Fingerprints.Imth |
| Use INDV fingerprint (indirect vector access) | Esolang.Funge.Fingerprints.Indv |
| Use LONG fingerprint (two-cell signed integers) | Esolang.Funge.Fingerprints.Long |
| Use MODU fingerprint (Modulo math) | Esolang.Funge.Fingerprints.Modu |
| Use RAND fingerprint (random numbers) | Esolang.Funge.Fingerprints.Rand |
| Use ROMA fingerprint (Roman numerals) | Esolang.Funge.Fingerprints.Roma |
| Use STCK fingerprint (stack manipulation) | Esolang.Funge.Fingerprints.Stck |
| Use STRN fingerprint (String manipulation) | Esolang.Funge.Fingerprints.Strn |
| Use TIME fingerprint (System time) | Esolang.Funge.Fingerprints.Time |
| Run Funge-98 from CLI | dotnet-funge |

## NuGet

| Project | NuGet | Summary |
|---|---|---|
| [dotnet-funge](./Interpreter/README.md) | [![NuGet: dotnet-funge](https://img.shields.io/nuget/v/dotnet-funge?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/dotnet-funge/) | Funge-98 command-line interpreter. |
| [Esolang.Funge.Generator](./Generator/README.md) | [![NuGet: Esolang.Funge.Generator](https://img.shields.io/nuget/v/Esolang.Funge.Generator?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Generator/) | Funge-98 source generator. |
| [Esolang.Funge.Parser](./Parser/README.md) | [![NuGet: Esolang.Funge.Parser](https://img.shields.io/nuget/v/Esolang.Funge.Parser?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Parser/) | Funge-98 source parser. |
| [Esolang.Funge.Processor](./Processor/README.md) | [![NuGet: Esolang.Funge.Processor](https://img.shields.io/nuget/v/Esolang.Funge.Processor?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Funge.Processor/) | Funge-98 execution engine. |
| [Esolang.Funge.Abstractions](./Abstractions/README.md) | [![NuGet: Esolang.Funge.Abstractions](https://img.shields.io/nuget/v/Esolang.Funge.Abstractions?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Abstractions/) | Fingerprint abstractions and built-in fingerprints. |
| [Esolang.Funge.Fingerprints.Arry](./Fingerprints.Arry/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Arry](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Arry?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Arry/) | ARRY fingerprint (array-like space access). |
| [Esolang.Funge.Fingerprints.Base](./Fingerprints.Base/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Base](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Base?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Base/) | BASE fingerprint (base conversion I/O). |
| [Esolang.Funge.Fingerprints.Bool](./Fingerprints.Bool/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Bool](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Bool?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Bool/) | BOOL fingerprint (Boolean logic). |
| [Esolang.Funge.Fingerprints.Date](./Fingerprints.Date/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Date](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Date?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Date/) | DATE fingerprint (calendar arithmetic). |
| [Esolang.Funge.Fingerprints.File](./Fingerprints.File/README.md) | [![NuGet: Esolang.Funge.Fingerprints.File](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.File?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.File/) | FILE fingerprint (file I/O). |
| [Esolang.Funge.Fingerprints.Fixp](./Fingerprints.Fixp/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Fixp](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Fixp?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Fixp/) | FIXP fingerprint (fixed-point math). |
| [Esolang.Funge.Fingerprints.Frth](./Fingerprints.Frth/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Frth](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Frth?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Frth/) | FRTH fingerprint (Forth-style stack helpers). |
| [Esolang.Funge.Fingerprints.Imth](./Fingerprints.Imth/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Imth](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Imth?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Imth/) | IMTH fingerprint (integer math helpers). |
| [Esolang.Funge.Fingerprints.Indv](./Fingerprints.Indv/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Indv](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Indv?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Indv/) | INDV fingerprint (indirect vector access). |
| [Esolang.Funge.Fingerprints.Long](./Fingerprints.Long/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Long](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Long?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Long/) | LONG fingerprint (two-cell signed integers). |
| [Esolang.Funge.Fingerprints.Modu](./Fingerprints.Modu/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Modu](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Modu?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Modu/) | MODU fingerprint (Modulo math). |
| [Esolang.Funge.Fingerprints.Rand](./Fingerprints.Rand/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Rand](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Rand?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Rand/) | RAND fingerprint (random numbers). |
| [Esolang.Funge.Fingerprints.Roma](./Fingerprints.Roma/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Roma](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Roma?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Roma/) | ROMA fingerprint (Roman numerals). |
| [Esolang.Funge.Fingerprints.Stck](./Fingerprints.Stck/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Stck](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Stck?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Stck/) | STCK fingerprint (stack manipulation). |
| [Esolang.Funge.Fingerprints.Strn](./Fingerprints.Strn/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Strn](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Strn?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Strn/) | STRN fingerprint (String manipulation). |
| [Esolang.Funge.Fingerprints.Time](./Fingerprints.Time/README.md) | [![NuGet: Esolang.Funge.Fingerprints.Time](https://img.shields.io/nuget/v/Esolang.Funge.Fingerprints.Time?logo=nuget)](https://www.nuget.org/packages/Esolang.Funge.Fingerprints.Time/) | TIME fingerprint (System time). |

## Framework Support

| Project | Target frameworks |
|---|---|
| Esolang.Funge.Generator | netstandard2.0 |
| Esolang.Funge.Parser | net8.0, net9.0, net10.0, netstandard2.0 |
| Esolang.Funge.Processor | net8.0, net9.0, net10.0 |
| Esolang.Funge.Abstractions | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Arry | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Base | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Bool | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Date | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.File | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Frth | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Imth | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Long | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Modu | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Roma | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Stck | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Strn | netstandard2.0, netstandard2.1, net10.0 |
| Esolang.Funge.Fingerprints.Time | netstandard2.0, netstandard2.1, net10.0 |
| dotnet-funge | net8.0, net9.0, net10.0 |

## Changelog

- [CHANGELOG](./CHANGELOG.md)

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.

## See also

- [Funge-98 specification](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown)
- [Befunge-93 / Befunge-98 on Esolangs wiki](https://esolangs.org/wiki/Befunge)
