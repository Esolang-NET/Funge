# Esolang.Funge.Fingerprints.Dirf

Provides the `DIRF` fingerprint (`0x44495246`) for Funge-98: directory functions.

## Instructions

| Instruction | Description |
|---|---|
| `C` | Pop path string, change current directory (`Directory.SetCurrentDirectory`); reflect on error |
| `M` | Pop path string, create directory (`Directory.CreateDirectory`); reflect on error |
| `R` | Pop path string, remove directory (`Directory.Delete`); reflect on error |

## References

- [PyFunge DIRF fingerprint](https://pythonhosted.org/PyFunge/fingerprint/DIRF.html)
