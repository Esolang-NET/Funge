# Esolang.Funge.Fingerprints.Refc

Provides the `REFC` fingerprint (`0x52454643`) for Funge-98: referenced cells extension.

## Instructions

| Instruction | Description |
|---|---|
| `R` | Pop vector, store in global table, push new reference integer |
| `D` | Pop reference integer, push stored vector; reflect if invalid |

Requires `IFungeVectorContext`; reflect if unavailable.

## References

- [PyFunge REFC fingerprint](https://pythonhosted.org/PyFunge/fingerprint/REFC.html)
