# Esolang.Funge.Fingerprints.Rand

`RAND` fingerprint (handprint `0x52414E44`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 random-number extension.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `I` | Push a random integer in `[0, n)`, treating `n` as an unsigned cell. |
| `M` | Push `int.MaxValue`. |
| `R` | Push an `FPSP` random float in `[0, 1)` encoded in a single cell. |
| `S` | Reseed the shared random-number source from the popped cell. |
| `T` | Reseed the shared random-number source from time / implementation-defined entropy. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Rand
```

## References

- https://web.archive.org/web/20160301000000/http://www.rcfunge98.com/rcsfingers.html#RAND
- https://github.com/Deewiant/CCBI/blob/aceb487970b3b4a4d8590d65a16dcc525ac77805/src/ccbi/fingerprints/rcfunge98/rand.d
- https://github.com/mjuvekar7/the-holey-junkyard/blob/ae2ac8d30509585bdfad84cc04e07965c55955f8/befunge/pyfunge/docs/fingerprint/FPxP.rst

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new RandomFingerprint()];
}
```
