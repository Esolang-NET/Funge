# Esolang.Funge.Fingerprints.Imth

`IMTH` fingerprint (handprint `0x494D5448`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 integer math helper instructions.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Average the top `n` values, treating missing stack entries as `0`. |
| `B` | Absolute value. |
| `C` | Multiply by `100`. |
| `D` | Decrement toward zero. |
| `E` | Multiply by `10000`. |
| `F` | Factorial. Reflects on negative input. |
| `G` | Sign function (`-1`, `0`, `1`). |
| `H` | Multiply by `1000`. |
| `I` | Increment away from zero. |
| `L` | Arithmetic left shift. Negative counts reverse the shift direction. |
| `N` | Minimum of the top `n` values. Reflects when `n <= 0`. |
| `R` | Arithmetic right shift. Negative counts reverse the shift direction. |
| `S` | Sum the top `n` values, treating missing stack entries as `0`. |
| `T` | Multiply by `10`. |
| `U` | Print the top value as an unsigned decimal integer followed by a space. |
| `X` | Maximum of the top `n` values. Reflects when `n <= 0`. |
| `Z` | Negate. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Imth
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#IMTH
- https://raw.githubusercontent.com/Deewiant/CCBI/master/src/ccbi/fingerprints/rcfunge98/imth.d

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new IntegerMathFingerprint()];
}
```
