# Esolang.Funge.Fingerprints.Time

`TIME` fingerprint (handprint `0x54494D45`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides instructions for retrieving the current system date/time fields, with a switch between local time and GMT.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `D` | Push the current day of month. |
| `F` | Push the current day of year, zero-based. |
| `G` | Switch subsequent reads to GMT/UTC. |
| `H` | Push the current hour. |
| `L` | Switch subsequent reads to local time. |
| `M` | Push the current minute. |
| `O` | Push the current month. |
| `S` | Push the current second. |
| `W` | Push the current day of week (`1` = Sunday). |
| `Y` | Push the current year. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Time
```

## References

- https://web.archive.org/web/20230617132045/https://rcfunge98.com/rcsfingers.html#TIME

## Usage

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98", FingerprintsProvider = nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [new TimeFingerprint()];
}
```
