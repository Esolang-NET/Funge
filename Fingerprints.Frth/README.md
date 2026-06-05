# Esolang.Funge.Fingerprints.Frth

`FRTH` fingerprint (handprint `0x46525448`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 Forth-style stack helper instructions.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `D` | Push the current top-stack depth. |
| `L` | Forth-style roll. Positive counts move the indexed item to the top; negative counts move the top deeper, padding with zeroes when needed. |
| `O` | Forth `over` (`a b -- a b a`). |
| `P` | Forth `pick`. Negative indices reflect; out-of-range indices push `0`. |
| `R` | Forth `rot` (`a b c -- b c a`). |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Frth
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#FRTH
- https://raw.githubusercontent.com/Deewiant/CCBI/master/src/ccbi/fingerprints/rcfunge98/frth.d

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new ForthFingerprint()];
}
```
