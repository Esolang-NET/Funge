# Esolang.Funge.Fingerprints.ThreeDsp

Provides the `3DSP` fingerprint (`0x33445350`) for Funge-98: 3D space manipulation.

3D vectors: three FPSP cells, stack order x(bottom), y, z(top). Pop: z, y, x. Push: x, y, z.
4×4 matrix in funge space: 16 cells at (x0+col, y0+row, z0), each cell is an FPSP float.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Add vectors |
| `B` | Subtract vectors |
| `C` | Cross product |
| `D` | Dot product → FPSP |
| `L` | Length → FPSP |
| `M` | Component-wise multiply |
| `N` | Normalize (or zero if zero-length) |
| `P` | Copy 4×4 matrix in space; reflect if no IFungeSpaceContext/VectorContext |
| `R` | Write rotation matrix; reflect if axis not in {1,2,3} |
| `S` | Write scale matrix (diagonal sx,sy,sz,1) |
| `T` | Write translation matrix |
| `U` | Duplicate vector |
| `V` | Perspective: pop vec, push ax/az, ay/az (az==0 treated as 1) |
| `X` | Transform row vector by 4×4 matrix |
| `Y` | Multiply matrices: result = B*A, write to target |
| `Z` | Scale vector by scalar (FPSP) |

## References

- [PyFunge 3DSP fingerprint](https://pythonhosted.org/PyFunge/fingerprint/3DSP.html)
