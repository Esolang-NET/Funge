# Esolang.Funge.Abstractions

Shared abstractions and built-in fingerprint implementations for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown).

## Overview

This package provides the interfaces and types needed to implement custom Funge-98 fingerprints,
and ships a set of standard fingerprints that have no external dependencies.

### Core types

| Type | Description |
|---|---|
| `IFingerprint` | Interface for fingerprint implementations |
| `IFungeExecutionContext` | Execution context passed to each fingerprint instruction |
| `IFungeRandomContext` | Optional capability exposing the runtime shared random-number source |
| `IFungeStackContext` | Optional capability exposing the current top-stack depth |
| `FingerprintInstruction` | Delegate type for a single instruction handler |
| `FingerprintHandprint` | Utility to compute a handprint integer from a 4-letter name |
| `FingerprintBuilder` | Fluent builder for `IReadOnlyDictionary<char, FingerprintInstruction>` |
| `NullFingerprint` | Built-in `NULL` fingerprint (all 26 instructions reflect) |

## Built-in Fingerprints

| Fingerprint | Handprint | `A`–`Z` behaviour |
|---|---|---|
| `NULL` | `0x4E554C4C` | All 26 instructions reflect the IP |

## Installation

```bash
dotnet add package Esolang.Funge.Abstractions
```

## Usage

### Using the built-in NULL fingerprint

```csharp
using Esolang.Funge;

// Singleton — no allocation needed
IFingerprint nullFp = NullFingerprint.Instance;
```

### Implementing a custom fingerprint

```csharp
using Esolang.Funge;

public sealed class MyFingerprint : IFingerprint
{
    public static readonly MyFingerprint Instance = new();

    // "MYFP" → 0x4D594650
    public int Handprint { get; } = FingerprintHandprint.Compute("MYFP");

    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } =
        new FingerprintBuilder()
            .Add('A', static ctx => ctx.Push(42))
            .Add('B', static ctx => ctx.Reflect())
            // ...
            .BuildInstructions();
}
```

### Registering with the Generator

```csharp
using Esolang.Funge;

partial class FungeSample
{
    [GenerateFungeMethod("program.b98")]
    [FingerprintsProvider(nameof(GetFingerprints))]
    public static partial string Run();

    static IEnumerable<IFingerprint> GetFingerprints()
        => [NullFingerprint.Instance];
}
```

## Target Frameworks

`netstandard2.0` · `netstandard2.1` · `net10.0`

AOT / trimming compatible.
