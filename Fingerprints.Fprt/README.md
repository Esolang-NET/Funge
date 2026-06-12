# Esolang.Funge.Fingerprints.Fprt

Provides the `FPRT` fingerprint (`0x46505254`) for Funge-98: formatted print.

All instructions pop a format string (0gnirts), pop the value, and push the formatted result (0gnirts).
Reflect if the format has 0 or 2+ format specifiers (detected via regex `%[^%]*[diouxXeEfgGsq]`).

## Instructions

| Instruction | Description |
|---|---|
| `I` | Pop format_s, pop n(int) → format integer |
| `F` | Pop format_s, pop x_fp(FPSP one cell) → format as single float |
| `D` | Pop format_s, pop x_fp(FPDP two cells) → format as double |
| `L` | Pop format_s, pop x_L(LONG two cells: high then low) → format as long |
| `S` | Pop format_s, pop str_s → format string |

## References

- [PyFunge FPRT fingerprint](https://pythonhosted.org/PyFunge/fingerprint/FPRT.html)
