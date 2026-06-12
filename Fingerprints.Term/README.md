# Esolang.Funge.Fingerprints.Term

Provides the `TERM` fingerprint (`0x5445524D`) for Funge-98: terminal ANSI escape sequences.

## Instructions

| Instruction | Description |
|---|---|
| `C` | Clear screen (`\x1b[2J`) |
| `D` | Cursor down n rows (pop n; n>0 down, n<0 up) |
| `G` | Go to position (pop c, pop r → `\x1b[r+1;c+1H`) |
| `H` | Home cursor (`\x1b[H`) |
| `L` | Clear to end of line (`\x1b[K`) |
| `S` | Clear to end of screen (`\x1b[J`) |
| `U` | Cursor up n rows (pop n; n>0 up, n<0 down) |

All instructions require `IFungeOutputContext`; reflect if unavailable.

## References

- [PyFunge TERM fingerprint](https://pythonhosted.org/PyFunge/fingerprint/TERM.html)
