# Esolang.Funge.Fingerprints.Long

`LONG` fingerprint (handprint `0x4C4F4E47`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 two-cell signed integer extension.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Add two long integers. |
| `B` | Absolute value. |
| `D` | Divide two long integers. Division by zero yields zero. |
| `E` | Sign-extend a single cell to a long integer. |
| `L` | Arithmetic left shift. Negative counts shift right. |
| `M` | Multiply two long integers. |
| `N` | Negate. |
| `O` | Modulo. Division by zero yields zero. |
| `P` | Print a long integer followed by a space. |
| `R` | Arithmetic right shift. Negative counts shift left. |
| `S` | Subtract two long integers. |
| `Z` | Parse a `0gnirts` decimal integer into a long integer. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Long
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#LONG
- https://raw.githubusercontent.com/Deewiant/CCBI/master/src/ccbi/fingerprints/rcfunge98/long_.d

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new LongIntegerFingerprint()];
}
```
