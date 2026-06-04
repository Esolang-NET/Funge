# Esolang.Funge.Fingerprints.Time

`TIME` fingerprint (handprint `0x54494D45`) for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

Provides instructions for retrieving the current system time and date.

## Supported Instructions

| Instruction | Behaviour |
|---|---|
| `D` | Date. Pushes Day (1-31), Month (1-12), and Year. |
| `G` | GMT. Pushes Seconds, Minutes, Hours, Day, Month, and Year in UTC. |
| `H` | Hour. Pushes the current local Hour (0-23). |
| `M` | Minute. Pushes the current local Minute (0-59). |
| `S` | Second. Pushes the current local Second (0-59). |
| `T` | Time. Pushes local Seconds, Minutes, and Hours. |
| `Y` | Year. Pushes the current local Year. |

## Installation

```bash
dotnet add package Esolang.Funge.Fingerprints.Time
```

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
