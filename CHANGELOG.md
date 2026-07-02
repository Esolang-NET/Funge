# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added

- `Esolang.Funge.Fingerprints.ThreeDsp`: new package implementing the `3DSP` fingerprint (`0x33445350`) with instructions `A` (add), `B` (subtract), `C` (cross product), `D` (dot product), `L` (length), `M` (component multiply), `N` (normalize), `P` (copy matrix), `R` (rotation matrix), `S` (scale matrix), `T` (translation matrix), `U` (duplicate), `V` (perspective), `X` (transform vector), `Y` (multiply matrices), `Z` (scale), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Cpli`: new package implementing the `CPLI` fingerprint (`0x43504C49`) with instructions `A` (add), `D` (divide), `M` (multiply), `O` (output), `S` (subtract), `V` (magnitude), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Dirf`: new package implementing the `DIRF` fingerprint (`0x44495246`) with instructions `C` (change directory), `M` (make directory), `R` (remove directory), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Evar`: new package implementing the `EVAR` fingerprint (`0x45564152`) with instructions `G` (get variable), `N` (count variables), `P` (put variable), `V` (get variable by index), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Fpdp`: new package implementing the `FPDP` fingerprint (`0x46504450`) with instructions `A` (add), `B` (sin), `C` (cos), `D` (divide), `E` (asin), `F` (from int), `G` (atan), `H` (acos), `I` (truncate), `K` (ln), `L` (log10), `M` (multiply), `N` (negate), `P` (print), `Q` (sqrt), `S` (subtract), `T` (tan), `V` (abs), `X` (exp), `Y` (pow), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Fprt`: new package implementing the `FPRT` fingerprint (`0x46505254`) with instructions `D` (format double), `F` (format float), `I` (format integer), `L` (format long), `S` (format string), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Fpsp`: new package implementing the `FPSP` fingerprint (`0x46505350`) with instructions `A` (add), `B` (sin), `C` (cos), `D` (divide), `E` (asin), `F` (from int), `G` (atan), `H` (acos), `I` (truncate), `K` (ln), `L` (log10), `M` (multiply), `N` (negate), `P` (print), `Q` (sqrt), `S` (subtract), `T` (tan), `V` (abs), `X` (exp), `Y` (pow), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Hrti`: new package implementing the `HRTI` fingerprint (`0x48525449`) with instructions `E` (erase mark), `G` (granularity), `M` (mark), `S` (second microseconds), `T` (time since mark), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Ical`: new package implementing the `ICAL` fingerprint (`0x4943414C`) with instructions `A` (unary and), `F` (forget), `I` (mingle), `N` (next), `O` (unary or), `R` (resume), `S` (select), `X` (unary xor), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Jstr`: new package implementing the `JSTR` fingerprint (`0x4A535452`) with instructions `G` (get string), `P` (put string), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Orth`: new package implementing the `ORTH` fingerprint (`0x4F525448`) with instructions `A` (bitwise and), `E` (bitwise xor), `G` (get cell), `O` (bitwise or), `P` (put cell), `S` (write string), `V` (set delta x), `W` (set delta y), `X` (set position x), `Y` (set position y), `Z` (skip if zero), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Refc`: new package implementing the `REFC` fingerprint (`0x52454643`) with instructions `D` (dereference), `R` (remember), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Sets`: new package implementing the `SETS` fingerprint (`0x53455453`) with instructions `A` (add element), `C` (count), `D` (duplicate), `G` (get from space), `I` (intersect), `M` (member), `P` (print), `R` (remove element), `S` (subtract), `U` (union), `W` (write to space), `X` (exchange), `Z` (discard), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Term`: new package implementing the `TERM` fingerprint (`0x5445524D`) with instructions `C` (clear screen), `D` (cursor down), `G` (goto position), `H` (home), `L` (clear to end of line), `S` (clear to end of screen), `U` (cursor up), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Toys`: new package implementing the `TOYS` fingerprint (`0x544F5953`) with instructions `A`–`Z` (26 instructions: replicate, butterfly, copy ascending, decrement, sum, fill from stack, get to stack, shift, increment, shift column, copy descending, peek left, move ascending, negate, shift row, product, set previous, peek right, fill space, set direction, random direction, move descending, wait, increment X, increment Y, increment Z), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Abstractions`: new package providing fingerprint abstractions (`IFingerprint`, `NullFingerprint`) targeting `netstandard2.0`, `netstandard2.1`, and `net10.0` with AOT compatibility.
- `Esolang.Funge.Fingerprints.File`: new package implementing the `FILE` fingerprint (`0x46494C45`) with instructions `C` (close), `D` (delete), `G` (get binary), `M` (move), `O` (open), `P` (put string), `R` (read), `S` (size), `W` (write byte), targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Fixp`: new package implementing the `FIXP` fingerprint (`0x46495850`) with instructions `A`, `B`, `C`, `D`, `I`, `J`, `N`, `O`, `P`, `Q`, `R`, `S`, `T`, `U`, `V`, and `X`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Frth`: new package implementing the `FRTH` fingerprint (`0x46525448`) with instructions `D`, `L`, `O`, `P`, and `R`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Imth`: new package implementing the `IMTH` fingerprint (`0x494D5448`) with instructions `A`, `B`, `C`, `D`, `E`, `F`, `G`, `H`, `I`, `L`, `N`, `R`, `S`, `T`, `U`, `X`, and `Z`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Indv`: new package implementing the `INDV` fingerprint (`0x494E4456`) with instructions `G`, `P`, `V`, and `W`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Long`: new package implementing the `LONG` fingerprint (`0x4C4F4E47`) with instructions `A`, `B`, `D`, `E`, `L`, `M`, `N`, `O`, `P`, `R`, `S`, and `Z`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Rand`: new package implementing the `RAND` fingerprint (`0x52414E44`) with instructions `I`, `M`, `R`, `S`, and `T`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Fingerprints.Stck`: new package implementing the `STCK` fingerprint (`0x5354434B`) with instructions `B`, `C`, `D`, `G`, `K`, `N`, `P`, `R`, `S`, `T`, `U`, `W`, and `Z`, targeting `netstandard2.0`, `netstandard2.1`, and `net10.0`.
- `Esolang.Funge.Generator`: new `[FingerprintsProvider]` attribute to register custom fingerprints; the generated runtime now dispatches fingerprint instructions through registered `IFingerprint` implementations.
- `Esolang.Funge.Processor`: fingerprint load/unload (`(` `)`) now dispatches instructions through registered `IFingerprint` implementations; unrecognised fingerprints reflect as per spec.

### Changed

- `Esolang.Funge.Parser`: `FungeParser.Parse(...)` and `FungeParser.ParseFile(...)` now accept an optional `CancellationToken` (default-enabled) and perform cooperative cancellation during parsing; `Esolang.Funge.Generator` and `Esolang.Funge.Interpreter` now propagate cancellation tokens into parser calls.
- `Esolang.Funge.Abstractions` / `Esolang.Funge.Processor` / `Esolang.Funge.Generator`: fingerprint execution now uses async instruction delegates (`Func<IFungeExecutionContext, ValueTask>`) and capability-aware execution contexts (including input/output and instruction-pointer lifecycle/context interfaces) for richer extension interoperability.
- `Esolang.Funge.Interpreter`: fingerprint CLI options now include explicit support for `--fingerprint-null` and `--fingerprint-file` alongside the existing built-in fingerprint option set.

## [2.0.0]- 2026-06-03

### Added

- `Esolang.Funge.Interpreter`: added `--path` / `-p` for file input and `--source` / `-s` for inline Funge-98 source, including multiline source text.
- `.github/workflows/dotnet.yml`: updated the CI tool E2E step to exercise the interpreter's new inline-source execution path.
- `Esolang.Funge.Generator`: documented the argument-binding heuristics for `string[]` / `IEnumerable<string>` parameters, including the `arg` / `env` name conventions used for `y`.
- `Esolang.Funge.Generator`: Added FG0011 diagnostic for enforcing partial method declaration.

## [1.1.1] - 2026-05-25

- `Esolang.Funge.Generator`: Implement logging support for runtime instructions and events (instruction execution, fingerprint operations).
- `Esolang.Funge.Generator`/`Processor`: Implement stub support for fingerprint instructions (`(` and `)`) to allow them to proceed without reflection, and fix corresponding test failures.

### Added

- `Esolang.Funge.Generator`: Generated Funge methods now support `string[]` or `IEnumerable<string>` parameters to pass custom command-line arguments and environment variables to the Funge program.
- `Esolang.Funge.Generator`: The `y` (System Information) instruction in the generated runtime now fully reports arguments and environment variables passed to the method, defaulting to host process data if `null`.
- `Esolang.Funge.Generator.Tests`: Added `Runtime_SystemInfo_ReportsCustomArgs` to verify custom argument passing via the `y` instruction.
- `Esolang.Funge.Generator.Tests`: Added coverage for `int` / `Task<int>` / `ValueTask<int>` signatures with explicit `TextWriter` and `PipeWriter` output, including runtime validation of exit-code returns plus pipe output.
- `Esolang.Funge.Processor.Tests`: Added unit tests for the `u` (Stack Stack Transfer) instruction.
- `Esolang.Funge.Processor.Tests`: Added unit tests for `StackStack` and `IP` functionality.

### Changed

- `Esolang.Funge.Generator`: The `=` (Execute) instruction in the generated runtime now uses `ProcessStartInfo.ArgumentList` on supported platforms (.NET Core 2.1+, .NET Standard 2.1+, .NET 5+) for improved security and robustness.
- `Esolang.Funge.Generator`: Cleaned up the `FungeRuntime.g.cs` template to remove redundant code and optimize generated output layout.
- `Esolang.Funge.Generator`: Explicit `TextWriter` / `PipeWriter` output parameters now compose with exit-code returns (`int`, `Task<int>`, `ValueTask<int>`), while `FG0007` remains reserved for return-based output conflicts (`string` / byte-sequence returns).
- `Esolang.Funge.Processor`: Refactored numerical (`.`) and character (`,`) output instructions for better consistency and maintainability.
- `Esolang.Funge.Processor`: Improved string mode handling and runtime output processing.
- `Esolang.Funge.Processor`: Fix `k` instruction logic and simplify fingerprints.
- Package metadata: added/expanded NuGet `PackageTags` for packable Funge packages (`Generator`, `Parser`, `Processor`, `dotnet-funge`) including `funge`, `funge-98`, and `befunge` tags for better discoverability.
- Code style: Use `var` instead of explicit types for improved readability.

### Fixed

- `Esolang.Funge.Generator.Tests`: Fixed incorrect test assertions in `FungeMethodGeneratorTests`.
- `Esolang.Funge.Processor`: Fix `0k` instruction test logic.

## [1.1.0] - 2026-05-08

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
- Build/package baseline: incremented `AssemblyVersion` / `FileVersion` to `1.1.0.3` and `Version` to `1.1.0`.

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

[Unreleased]: https://github.com/Esolang-NET/Funge/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/Esolang-NET/Funge/compare/v1.1.1...v2.0.0
[1.1.1]: https://github.com/Esolang-NET/Funge/tree/v1.1.1
[1.1.0]: https://github.com/Esolang-NET/Funge/tree/v1.1.0
[1.0.1]: https://github.com/Esolang-NET/Funge/tree/v1.0.1
[1.0.0]: https://github.com/Esolang-NET/Funge/tree/v1.0.0
