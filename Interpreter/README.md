# dotnet-funge

Command-line interpreter for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) programs.

## Installation

```
dotnet tool install -g dotnet-funge
```

## Usage

```
dotnet-funge <path>
```

| Argument | Description |
|---|---|
| `<path>` | Path to a Funge-98 source file (`.b98`) |

### Example

```
dotnet-funge hello.b98
```

Standard input / output are connected to the running program (`~` / `&` for input, `,` / `.` for output).

The process exit code reflects the value passed to `q`; it is `0` if the program ends without `q`.

## Funge-98 Compliance

Delegates execution to `Esolang.Funge.Processor`, including Trefunge 3D directions (`h`/`l`/`m`).  
For detailed processor-level behavior, refer to the processor package documentation.

| Area | Status |
|---|---|
| Core instruction set (stack, arithmetic, comparison, direction, I/O, storage, movement) | ✅ |
| Funge-98 extensions (`k` iterate, `t` concurrency, `{`/`}`/`u` stack stack) | ✅ |
| System info (`y`) | 🟡 env vars / command-line args are empty |
| Standard I/O (`&` `~` `,` `.`) connected to stdin / stdout | ✅ |
| Exit code via `q` | ✅ |
| Fingerprints (`(` `)` `A`–`Z`) | ❌ reflects (not implemented) |
| File I/O (`i` `o`) | ✅ |
| System exec (`=`) | ✅ |
| 3D / Trefunge (`h` `l` `m`) | ✅ |

## References

- [Funge-98 Specification](https://codeberg.org/catseye/Funge-98/src/branch/master/doc/funge98.markdown) — Chris Pressey, Cat's Eye Technologies
- [Funge-98 — Esolangs Wiki](https://esolangs.org/wiki/Funge-98)
- [Mycology — Funge-98 compliance test suite](https://github.com/Deewiant/Mycology)

## Target Frameworks

`net8.0` · `net9.0` · `net10.0`
