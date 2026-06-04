# Esolang.Funge.Fingerprints.Strn

`STRN` fingerprint (handprint `0x5354524E`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides string manipulation instructions. Strings are represented as null-terminated sequences of values on the stack (0gnirts).

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Append. Pops S1 (top) then S2 (bottom), pushes S2 + S1. |
| `C` | Compare. Pops S1 (top) then S2 (bottom), pushes 1 if S1 > S2, -1 if S1 < S2, 0 if equal. |
| `L` | Length. Pops S, pushes its length. |
| `N` | Number to string. Pops n, pushes its decimal string representation. |
| `R` | Reverse. Pops S, pushes its reversed version. |
| `S` | String to number. Pops S, parses as integer, pushes result. Reflects on error. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Strn
```

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new StringFingerprint()];
}
```
