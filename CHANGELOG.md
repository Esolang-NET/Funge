# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added

- `Esolang.Processor.Abstractions` (`Esolang.Processor` namespace): shared execution abstractions package (`IProcessor<TProgram>`, `ITextProcessor<TProgram>`, `IPipeProcessor<TProgram>`).
- `Esolang.Funge.Processor.Tests`: coverage for `RunToEnd(...)` and `RunToEndAsync(...)` on `FungeProcessor`.
- `Esolang.Funge.Generator.Tests`: 3D runtime coverage for `h` / `l` / `m` and 3D `g` / `p` behavior.
- `samples/Generator.UseConsole/Programs/hello3d.b98`: minimal Trefunge sample using form-feed (`\f`) layer separation.
- `Esolang.Funge.Generator.Tests`: runtime coverage for storage offset-aware `g` / `p`, stack stack transfer via `u`, and `y` capability flags.

### Changed

- `Esolang.Funge.Processor`: `FungeProcessor` now implements `ITextProcessor<FungeSpace>` and exposes `RunToEnd(...)` / `RunToEndAsync(...)` while preserving existing `Run(...)` behavior.
- `Esolang.Funge.Processor`: switched abstraction source from local `Processor/IProcessor.cs` to `Esolang.Processor.Abstractions` package.
- `dotnet-funge` (`Esolang.Funge.Interpreter`): command execution path now calls `RunToEnd(...)`.
- `Esolang.Funge.Generator`: added return-type support for `int`, `Task<int>`, and `ValueTask<int>`, and aligned `q` handling to return the popped exit code (`@` returns `0`).
- `Esolang.Funge.Parser`: parser and space model now support 3D coordinates (`X`, `Y`, `Z`), with form-feed (`\f`) treated as a Z-layer separator.
- `Esolang.Funge.Processor`: enabled Trefunge 3D navigation instructions `h` / `l` / `m`, extended `?` to 6 directions, and upgraded `g` / `p` / `x` to 3D operands.
- `Esolang.Funge.Processor`: implemented filesystem instructions `i` / `o` (file input/output).
- `Esolang.Funge.Processor`: implemented system execution instruction `=` (returns process exit code on stack).
- `Esolang.Funge.Generator`: generated runtime now uses 3D execution space (XYZ bounds/cells), supports `h` / `l` / `m`, and handles 3D `g` / `p` / `x` semantics.
- `Esolang.Funge.Generator`: generated runtime now supports filesystem instructions `i` / `o`.
- `Esolang.Funge.Generator`: generated runtime now supports system execution instruction `=`.
- `Esolang.Funge.Processor`: `y` now includes command-line arguments and environment variables, uses Funge-98 date/time encoding (base-256), reports least-stack-area bounds as relative extents, and aligns positive-`c` pick behavior with full-stack semantics.
- `dotnet-funge` (`Esolang.Funge.Interpreter`): processor construction now passes command-line arguments and environment variables for `y` system-info reporting.
- `Esolang.Funge.Generator`: generated runtime now supports concurrency (`t`), stack stack operations (`{` / `}` / `u`), `y` system-info, and applies storage offset semantics to `g` / `p` and file I/O least-point handling.
- `Esolang.Funge.Generator`: method return handling now dispatches via runtime facade APIs (`RunSync` / `RunTask*` / `RunValueTask*` / `RunEnumerable` / `RunAsyncEnumerable`), and synchronous signatures can cooperatively cancel infinite execution via `CancellationToken`.
- `Esolang.Funge.Generator`: runtime source emission now includes only the facade methods required by generated signatures (Piet-style minimal runtime emission), and facade source blocks are generated with raw string literals for cleaner output layout.
- `Esolang.Funge.Generator`: generated runtime class and internal runtime entry methods are now annotated with `EditorBrowsable(EditorBrowsableState.Never)` to reduce IntelliSense surface for internal-use APIs.
- Docs: updated package README compliance tables and added 3D notes/examples (including `\f` layer separator guidance).
- Repository hygiene: ignore `*.csproj.lscache` (C# Dev Kit cache) to avoid generated noise in working trees.

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
