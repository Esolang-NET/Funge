# dotnet-funge

Command-line interpreter for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) (Befunge-98) programs.

## Installation

```
dotnet tool install -g dotnet-funge
```

## Usage

```
dotnet funge <path>
```

| Argument | Description |
|---|---|
| `<path>` | Path to a Funge-98 source file (`.b98`) |

### Example

```
dotnet funge hello.b98
```

Standard input / output are connected to the running program (`~` / `&` for input, `,` / `.` for output).

The process exit code reflects the value passed to `q`; it is `0` if the program ends without `q`.

## Target Frameworks

`net8.0` · `net9.0` · `net10.0`
