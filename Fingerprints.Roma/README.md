# Esolang.Funge.Fingerprints.Roma

`ROMA` fingerprint (handprint `0x524F4D41`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides Roman numeral values as instructions.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `C` | Push 100 |
| `D` | Push 500 |
| `I` | Push 1 |
| `L` | Push 50 |
| `M` | Push 1000 |
| `V` | Push 5 |
| `X` | Push 10 |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Roma
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#ROMA

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new RomanFingerprint()];
}
```
