# Esolang.Funge.Fingerprints.Stck

`STCK` fingerprint (handprint `0x5354434B`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides the Rc/Funge-98 stack manipulation extension.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `B` | Bury a value `n` deep in the stack. Negative `n` inserts zeroes above the value. |
| `C` | Push the current top-stack depth. |
| `D` | Duplicate the top `n` stack values. |
| `G` | Read `n` stack entries from Funge-space using `Vsrc`, `Vdlt`, and the storage offset. |
| `K` | Move a stack block `[en..st]` to the top of the stack. |
| `N` | Reverse the top `n` stack values. |
| `P` | Print the current stack contents non-destructively. |
| `R` | Reverse the entire top stack. |
| `S` | Duplicate the second item on the stack. |
| `T` | Swap the second and third items on the stack. |
| `U` | Drop items until the requested value is found, then push the drop count. |
| `W` | Write `n` stack entries to Funge-space using `Vsrc`, `Vdlt`, and the storage offset. |
| `Z` | Convert a `0string` stack segment into standard `0gnirts` order. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Stck
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#STCK
- https://raw.githubusercontent.com/catseye/Funge-98/master/doc/funge98.markdown

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new StackManipulationFingerprint()];
}
```
