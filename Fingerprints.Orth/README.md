# Esolang.Funge.Fingerprints.Orth

Provides the `ORTH` fingerprint (`0x4F525448`) for Funge-98: orthogonal easement library.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Bitwise AND: pop b, pop a → push (a & b) |
| `E` | Bitwise XOR: pop b, pop a → push (a ^ b) |
| `G` | Get cell: pop x, pop y → push cell at (x,y,0); reflect if no IFungeSpaceContext |
| `O` | Bitwise OR: pop b, pop a → push (a \| b) |
| `P` | Put cell: pop value, pop x, pop y → set cell at (x,y,0); reflect if no IFungeSpaceContext |
| `S` | Write string (0gnirts); reflect if no IFungeOutputContext |
| `V` | Set Delta.X to popped value; reflect if no IFungePositionContext |
| `W` | Set Delta.Y to popped value; reflect if no IFungePositionContext or Dimensions<2 |
| `X` | Set Position.X to popped value; reflect if no IFungePositionContext |
| `Y` | Set Position.Y to popped value; reflect if no IFungePositionContext or Dimensions<2 |
| `Z` | Skip next (advance by delta) if popped value==0; reflect if no IFungePositionContext |

## References

- [PyFunge ORTH fingerprint](https://pythonhosted.org/PyFunge/fingerprint/ORTH.html)
