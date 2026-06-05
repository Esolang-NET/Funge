# Esolang.Funge.Fingerprints.Arry

`ARRY` fingerprint (handprint `0x41525259`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides array-like access helpers over funge-space using the current vector as the base coordinate.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Store one value at `Va + x`. |
| `B` | Read one value from `Va + x`. |
| `C` | Store one value at `Va + (x, y)`. |
| `D` | Read one value from `Va + (x, y)`. |
| `E` | Store one value at `Va + (x, y, z)`. |
| `F` | Read one value from `Va + (x, y, z)`. |
| `G` | Push the runtime funge-space dimensionality. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Arry
```

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new ArrayFingerprint()];
}
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#ARRY
