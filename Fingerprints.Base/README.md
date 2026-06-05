# Esolang.Funge.Fingerprints.Base

`BASE` fingerprint (handprint `0x42415345`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides numeric base conversion I/O instructions.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `B` | Output the top stack value in binary. |
| `H` | Output the top stack value in hexadecimal. |
| `I` | Read one input line using the specified base and push the parsed value. |
| `N` | Output `n` in base `b`. |
| `O` | Output the top stack value in octal. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Base
```

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new BaseFingerprint()];
}
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#BASE
