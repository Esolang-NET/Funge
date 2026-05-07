# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added

- `Esolang.Processor.Abstractions` (`Esolang.Processor` namespace): shared execution abstractions package (`IProcessor<TProgram>`, `ITextProcessor<TProgram>`, `IPipeProcessor<TProgram>`).
- `Esolang.Funge.Processor.Tests`: coverage for `RunToEnd(...)` and `RunToEndAsync(...)` on `FungeProcessor`.

### Changed

- `Esolang.Funge.Processor`: `FungeProcessor` now implements `ITextProcessor<FungeSpace>` and exposes `RunToEnd(...)` / `RunToEndAsync(...)` while preserving existing `Run(...)` behavior.
- `Esolang.Funge.Processor`: switched abstraction source from local `Processor/IProcessor.cs` to `Esolang.Processor.Abstractions` package.
- `dotnet-funge` (`Esolang.Funge.Interpreter`): command execution path now calls `RunToEnd(...)`.
- `Esolang.Funge.Generator`: added return-type support for `int`, `Task<int>`, and `ValueTask<int>`, and aligned `q` handling to return the popped exit code (`@` returns `0`).

## [1.0.1] - 2026-05-07

### Changed

- `Esolang.Funge.Generator`, `Esolang.Funge.Parser`, `Esolang.Funge.Processor`: package metadata now includes `PackageReadmeFile` and packs each project `README.md`.
- Build/package baseline: incremented `AssemblyVersion` / `FileVersion` to `1.0.1.2` and `Version` to `1.0.1`.

## [1.0.0] - 2026-05-06

### Added

- Initial implementation of Funge-98 (Befunge-98) parser, processor and interpreter.
- `Esolang.Funge.Parser`: FungeSpace (sparse infinite 2D grid), FungeVector, FungeParser.
- `Esolang.Funge.Processor`: FungeProcessor supporting core Funge-98 instruction set, stack stack, concurrent IPs.
- `dotnet-funge`: Command-line interpreter for `.b98` files.

### Changed

- Build/package baseline: incremented `AssemblyVersion` / `FileVersion` to `1.0.0.1`.
- `dotnet-funge`: enabled trimming/AOT analyzer-related properties and marked tool package as AOT-compatible for `net8.0+`.
- `dotnet-funge`: package metadata now includes `PackageReadmeFile` and packs `Interpreter/README.md`.
- `Esolang.Funge.Generator`: `FG0008` / `FG0009` severity changed from Warning to Info.
- `Esolang.Funge.Generator`: runtime now throws when input/output instructions are executed without a declared input/output interface.
