# Esolang.Funge.Fingerprints.Fixp

`FIXP` fingerprint (handprint `0x46495850`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 fixed-point math extension.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Bitwise AND. |
| `B` | Arc cosine: input/output use scale `10000`, output in degrees. |
| `C` | Cosine: input in scaled degrees, output scaled by `10000`. |
| `D` | Random integer in `[0,n)` for positive `n`, `[n,0)` for negative `n`, or `0` for `n = 0`. |
| `I` | Sine: input in scaled degrees, output scaled by `10000`. |
| `J` | Arc sine: input/output use scale `10000`, output in degrees. |
| `N` | Negate. |
| `O` | Bitwise OR. |
| `P` | Multiply by `pi`, truncating any fractional part. |
| `Q` | Integer square root, truncating any fractional part. |
| `R` | Integer power with negative exponents collapsing to `0` except for bases `1` and `-1`. |
| `S` | Sign (`-1`, `0`, `1`). |
| `T` | Tangent: input in scaled degrees, output scaled by `10000`. |
| `U` | Arc tangent: input/output use scale `10000`, output in degrees. |
| `V` | Absolute value. |
| `X` | Bitwise XOR. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Fixp
```

## References

- https://web.archive.org/web/20160305031427/http://www.rcfunge98.com/rcsfingers.html#FIXP
- https://web.archive.org/web/20160305031427/http://www.rcfunge98.com/rcfunge2_manual.html#FIXP
- https://github.com/Deewiant/CCBI/blob/aceb487970b3b4a4d8590d65a16dcc525ac77805/src/ccbi/fingerprints/rcfunge98/fixp.d

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new FixedPointFingerprint()];
}
```
