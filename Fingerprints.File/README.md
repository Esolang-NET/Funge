# Esolang.Funge.Fingerprints.File

`FILE` fingerprint (handprint `0x46494C45`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides file I/O operations via the standard `FILE` fingerprint.
File handles are tracked per `FileFingerprint` instance; dispose the instance to release all open handles.

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.File
```

## Instructions

| Instruction | Behaviour |
|---|---|
| `C` | Close file. Pop handle. |
| `D` | Delete file. Pop 0gnirts filename. Reflect on error. |
| `G` | Get string (one line). Pop handle. Push 0gnirts. Reflect on EOF or error. |
| `M` | Move (rename) file. Pop 0gnirts dest, pop 0gnirts src. Reflect on error. |
| `O` | Open file. Pop flags, pop 0gnirts filename. Push handle. Reflect on error. |
| `P` | Put string. Pop handle, pop 0gnirts. Write UTF-8 to file. Reflect on error. |
| `R` | Read byte. Pop handle. Push byte value. Reflect on EOF or error. |
| `S` | File size. Pop 0gnirts filename. Push size as integer. Reflect on error. |
| `W` | Write byte. Pop handle, pop byte value. Write to file. Reflect on error. |

### Open flags (`O`)

| Flag | Mode |
|---|---|
| `0` | Read (open existing) |
| `1` | Write (create or truncate) |
| `2` | Append (create or append) |

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98")]
    [FingerprintsProvider(nameof(GetFingerprints))]
    public static partial string Run();

    IEnumerable<IFingerprint> GetFingerprints()
        => [new FileFingerprint()];
}
```

> **Note:** `FileFingerprint` implements `IDisposable`. Dispose the instance (or use `using`) to ensure all open file handles are released.

## Target Frameworks

`netstandard2.0` · `netstandard2.1` · `net10.0`

AOT / trimming compatible.
