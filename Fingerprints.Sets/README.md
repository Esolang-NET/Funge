# Esolang.Funge.Fingerprints.Sets

Provides the `SETS` fingerprint (`0x53455453`) for Funge-98: set operations.

Sets are represented on the stack as elements (any order) followed by count on top.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Pop value, pop set → push (set ∪ {value}) |
| `C` | Peek set → push set, push count |
| `D` | Pop set → push set, push set (duplicate) |
| `G` | Pop delta_v, source_v → read set from space; reflect if invalid or no IFungeSpaceContext/VectorContext |
| `I` | Pop b, pop a → push (a ∩ b) |
| `M` | Pop set, pop value → push 1 if value in set else 0 |
| `P` | Pop set → print; reflect if no IFungeOutputContext |
| `R` | Pop set, pop value → push (set - {value}) |
| `S` | Pop b, pop a → push (a - b) |
| `U` | Pop b, pop a → push (a ∪ b) |
| `W` | Pop dest_v, pop delta_v, pop set → write to space; push set back |
| `X` | Pop b, pop a → push b, push a (exchange) |
| `Z` | Pop set → discard |

## References

- [PyFunge SETS fingerprint](https://pythonhosted.org/PyFunge/fingerprint/SETS.html)
