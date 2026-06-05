# Esolang.Funge.Fingerprints.Bool

`BASE` fingerprint (handprint `0x42415345`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides I/O for numbers in other bases.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `B` |   |
| `H` |   |
| `I` |   |
| `N` |   |
| `O` |   |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Bool
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

## See also

https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#BASE
