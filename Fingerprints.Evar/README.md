# Esolang.Funge.Fingerprints.Evar

Provides the `EVAR` fingerprint (`0x45564152`) for Funge-98: environment variables extension.

## Instructions

| Instruction | Description |
|---|---|
| `G` | Pop name string, push value string (empty if not found) |
| `N` | Push count of environment variables |
| `P` | Pop "name=value" string, set environment variable; reflect if no '=' |
| `V` | Pop index i, push i-th env var sorted by key as "name=value"; reflect if out of range |

## References

- [PyFunge EVAR fingerprint](https://pythonhosted.org/PyFunge/fingerprint/EVAR.html)
