# Esolang.Funge.Fingerprints.Fpsp

Provides the `FPSP` fingerprint (`0x46505350`) for Funge-98: single precision floating point.

One stack cell = IEEE 754 float via `BitConverter.SingleToInt32Bits` / `Int32BitsToSingle`.

## Instructions

| Instruction | Stack | Description |
|---|---|---|
| `A` | x y → (x+y) | Add |
| `B` | x → sin(x) | Sine |
| `C` | x → cos(x) | Cosine |
| `D` | x y → (x/y) | Divide |
| `E` | x → asin(x) | Arc sine |
| `F` | n → (float)n | Integer to float |
| `G` | x → atan(x) | Arc tangent |
| `H` | x → acos(x) | Arc cosine |
| `I` | x → (int)trunc(x) | Float to int |
| `K` | x → ln(x) | Natural log |
| `L` | x → log10(x) | Base-10 log |
| `M` | x y → (x*y) | Multiply |
| `N` | x → (-x) | Negate |
| `P` | x → | Print; reflect if no IFungeOutputContext |
| `Q` | x → sqrt(x) | Square root |
| `S` | x y → (x-y) | Subtract |
| `T` | x → tan(x) | Tangent |
| `V` | x → abs(x) | Absolute value |
| `X` | x → exp(x) | Exponential |
| `Y` | x y → (y^x) | Power (x on top) |

## References

- [PyFunge FPxP fingerprint](https://pythonhosted.org/PyFunge/fingerprint/FPxP.html)
