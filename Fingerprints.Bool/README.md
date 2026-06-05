# Esolang.Funge.Fingerprints.Bool

`BOOL` fingerprint (handprint `0x424F4F4C`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides instructions for Boolean logical operations.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Logical AND. Pops `b`, pops `a`. If both `a` and `b` are non-zero, pushes `1`, else `0`. |
| `N` | Logical NOT. Pops `a`. If `a` is zero, pushes `1`, else `0`. |
| `O` | Logical OR. Pops `b`, pops `a`. If either `a` or `b` is non-zero, pushes `1`, else `0`. |
| `X` | Logical XOR. Pops `b`, pops `a`. If exactly one of `a` and `b` is non-zero, pushes `1`, else `0`. |

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
        => [new BoolFingerprint()];
}
```
