# Esolang.Funge.Fingerprints.Modu

`MODU` fingerprint (handprint `0x4D4F4455`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides specialized modulo arithmetic instructions.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `M` | Signed-result modulo. Result has the same sign as the divisor. |
| `R` | C-language remainder. Result has the same sign as the dividend. |
| `U` | Unsigned-result modulo. Result is always non-negative. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Modu
```

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new ModuloFingerprint()];
}
```
