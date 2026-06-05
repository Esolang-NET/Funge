# Esolang.Funge.Fingerprints.Date

`DATE` fingerprint (handprint `0x44415445`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides calendar arithmetic helpers such as Julian day conversion, day-of-year conversion, and date differences.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `A` | Add a day offset to a calendar date. |
| `C` | Convert a Julian day number to a calendar date. |
| `D` | Push the number of days between two calendar dates. |
| `J` | Convert a calendar date to a Julian day number. |
| `T` | Convert `(year, day-of-year)` to `(year, month, day)`. |
| `W` | Push the day of week (`0` = Monday). |
| `Y` | Push the zero-based day of year. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Date
```

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new DateFingerprint()];
}
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#DATE
