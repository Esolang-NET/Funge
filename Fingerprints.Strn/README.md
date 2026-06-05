# Esolang.Funge.Fingerprints.Strn

`STRN` fingerprint (handprint `0x5354524E`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides string manipulation instructions. Strings are represented as null-terminated sequences of values on the stack (0gnirts).

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Append two 0gnirts values. |
| `C` | Compare two 0gnirts values lexically and push the signed difference. |
| `D` | Output a 0gnirts value. |
| `F` | Search for one 0gnirts inside another and push the matching suffix, or an empty 0gnirts. |
| `G` | Read a null-terminated string from funge-space at `Va + storage offset`. |
| `I` | Read one input line as a 0gnirts value. |
| `L` | Push the leftmost `n` characters of a 0gnirts value. |
| `M` | Push a substring window from a 0gnirts value. |
| `N` | Duplicate a 0gnirts and push its length. |
| `P` | Write a 0gnirts to funge-space at `Va + storage offset`, including the terminator. |
| `R` | Push the rightmost `n` characters of a 0gnirts value. |
| `S` | Convert an integer to its decimal 0gnirts representation. |
| `V` | Parse a 0gnirts using `atoi`-style rules and push the integer value. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Strn
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#STRN

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
