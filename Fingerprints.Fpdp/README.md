# Esolang.Funge.Fingerprints.Fpdp

Provides the `FPDP` fingerprint (`0x46504450`) for Funge-98: double precision floating point.

Two cells per double: lower 32 bits pushed first, upper 32 bits second. Pop: pop upper first, then lower.

## Instructions

Same 19 instructions as FPSP but using double precision. See FPSP README for instruction descriptions.

| Instruction | Description |
|---|---|
| `A` | Add | `B` | Sin | `C` | Cos | `D` | Divide | `E` | Asin |
| `F` | Int to double | `G` | Atan | `H` | Acos | `I` | Truncate to int |
| `K` | Natural log | `L` | Log10 | `M` | Multiply | `N` | Negate |
| `P` | Print; reflect if no IFungeOutputContext |
| `Q` | Sqrt | `S` | Subtract | `T` | Tan | `V` | Abs | `X` | Exp | `Y` | Pow |

## References

- [PyFunge FPxP fingerprint](https://pythonhosted.org/PyFunge/fingerprint/FPxP.html)
