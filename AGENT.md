# Agent Guide

This file captures repository-specific guidance that became clear while implementing and validating built-in fingerprints.

It is intended to be the main agent-facing guide for this repository, especially when changing **fingerprints**, **Processor**, and **Generator** together.

## Testing

This project uses the Microsoft Testing Platform (MTP).

### Running Tests

To run all tests in the solution:

```bash
dotnet test
```

### Collecting Code Coverage

To run tests and collect code coverage:

```bash
dotnet test --coverage --coverage-output-format cobertura
```

To generate an HTML coverage report using ReportGenerator:

```bash
dotnet reportgenerator "-reports:**/*.cobertura.xml" "-targetdir:coveragereport" -reporttypes:Html
```

The report will be generated in the `coveragereport` directory.

## Logging Architecture

This project implements conditional logging in generated code.

- **Mechanism**: `MethodGenerator` inspects the target class for an `ILogger` or `ILogger<T>` field.
- **Pattern**: If detected, it injects zero-allocation logging code using `LoggerMessage.Define` inside the generated method body.
- **Compatibility**: To avoid hard dependencies in the generated `FungeRuntime` facade, facade methods keep their original signature. Internal runtime methods accept `object? logger`, which is conditionally cast and used only if available.

## Spec / Reference Priority

When implementing or correcting a fingerprint, use this priority order:

1. Archived Rc/Funge-98 fingerprint page
2. CCBI reference implementation
3. Mycology expected behavior / conformance hints
4. Secondary mirrors or wiki pages

If sources disagree, document the chosen behavior in the fingerprint `README.md`.

## Fingerprint Implementation Rules

- Keep `IFungeExecutionContext` minimal. Add new abilities as **optional capability interfaces** instead of growing the core interface.
- Required capabilities should be checked with `ctx is IFoo foo`; if unavailable, **reflect**.
- Avoid silent fallback behavior for unsupported runtime features.
- Keep Processor and Generator runtime behavior aligned. If a fingerprint needs a new capability, wire both runtimes or do not expose it.

## Existing Capability Interfaces

Current optional capabilities include:

- `IFungeInputContext`
- `IFungeOutputContext`
- `IFungeVectorContext`
- `IFungeSpaceContext`
- `IFungeStorageOffsetContext`
- `IFungeStackContext`
- `IFungeInstructionPointerContext`
- `IFungeInstructionPointerLifecycle`
- `IFungeRandomContext`

For Generator, optional interfaces must only be emitted when the corresponding abstraction type resolves through `KnownFungeTypes`.

## Mutable Fingerprint State

If a fingerprint has mutable runtime state, assume it must be **per instruction pointer**, not global.

- Store state keyed by `InstructionPointerId`
- Copy state on `t` via `IFungeInstructionPointerLifecycle.OnInstructionPointerCloned(...)`
- Clean up state on termination via `OnInstructionPointerTerminated(...)`

This was necessary for fingerprints such as `TIME` and `FILE`.

## Randomness

Random behavior should go through `IFungeRandomContext`.

- Core `?`
- `RAND`
- any future fingerprint random helpers

should share the same runtime RNG source so reseeding remains coherent across Processor and Generator.

## Vector / Space Notes

The runtime currently exposes vectors in `(x, y, z)` order.

- `PopVector()` pops `z`, then `y`, then `x`
- `PushVector(x, y, z)` pushes `x`, then `y`, then `z`

Be explicit about whether a fingerprint uses:

- absolute space coordinates
- storage-offset-relative coordinates
- fixed `+X` traversal for encoded vectors

Examples already implemented:

- `ARRY`: absolute coordinates
- `INDV`: storage-offset-relative indirect pointers; stored vectors are read/written along `+X`
- `STCK` `G/W`: storage-offset-relative funge-space access

## Generator-Specific Rules

- `FingerprintsProvider` must resolve to a member implicitly convertible to `IEnumerable<IFingerprint>`
- Invalid provider name or invalid provider return/property type should produce `FG0012`
- Add tests for both:
  - successful generation
  - invalid member / invalid method return type / invalid property type
  - missing optional abstraction types

## Test Expectations

For fingerprint work, prefer this validation order:

1. fingerprint package tests
2. `Processor.Tests`
3. `Generator.Tests`
4. `Interpreter.Tests`

When a fingerprint relies on a capability, add:

- unit tests in its own test project
- Processor integration coverage
- Generator functional coverage if generated runtime capability exposure matters

## Documentation Checklist

When adding a new fingerprint package, update all of:

- `Fingerprints.<Name>/README.md`
- root `README.md`
- `CHANGELOG.md`
- `Esolang.Funge.slnx`
- `Interpreter/FingerprintsOptions.cs`
- interpreter project references if the CLI should expose it

Each fingerprint README should include **reference URLs** for the spec / implementation sources used.
