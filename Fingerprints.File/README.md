# Esolang.Funge.Fingerprints.File

`FILE` fingerprint (handprint `0x46494C45`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides file I/O operations via the standard `FILE` fingerprint.
File handles are tracked per instruction pointer within a `FileFingerprint` instance; dispose the instance to release any remaining open handles.

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.File
```

## Instructions

| Instruction | Behaviour |
|---|---|
| `C` | Close a file handle. |
| `D` | Delete a file by path. |
| `G` | Read one line from a handle and push it as 0gnirts plus its byte length. |
| `L` | Push the current file position for a handle. |
| `O` | Open a file with a buffer vector captured from `Va + storage offset`. |
| `P` | Write a 0gnirts to a handle. |
| `R` | Read `n` bytes from a handle into the configured buffer vector in funge-space. |
| `S` | Seek a handle using mode `0`/`1`/`2` = begin/current/end. |
| `W` | Write `n` bytes from the configured buffer vector in funge-space to a handle. |

### Open flags (`O`)

| Flag | Mode |
|---|---|
| `0` | Read |
| `1` | Write (create or truncate) |
| `2` | Append |
| `3` | Read/write |
| `4` | Read/write (create or truncate) |
| `5` | Read/write append |

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

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#FILE
