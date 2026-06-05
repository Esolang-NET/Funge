# Esolang.Funge.Fingerprints.Indv

`INDV` fingerprint (handprint `0x494E4456`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 indirect vector extension.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `G` | Indirectly read a cell through a pointer vector stored in funge-space. |
| `P` | Indirectly write a cell through a pointer vector stored in funge-space. |
| `V` | Indirectly read a vector stored in funge-space. |
| `W` | Indirectly write a vector into funge-space. |

All pointer vectors use the current storage offset. Stored vectors are read and written along `+X` in logical order (`z,y,x` in 3D; `y,x` in 2D).

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Indv
```

## References

- https://web.archive.org/web/20230617133910/http://rcfunge98.com/rcsfingers.html#INDV
- https://pythonhosted.org/PyFunge/fingerprint/INDV.html
- https://raw.githubusercontent.com/Deewiant/CCBI/master/src/ccbi/fingerprints/rcfunge98/indv.d

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new IndirectVectorFingerprint()];
}
```
