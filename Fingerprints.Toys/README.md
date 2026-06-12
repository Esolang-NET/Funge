# Esolang.Funge.Fingerprints.Toys

Provides the `TOYS` fingerprint (`0x544F5953`) for Funge-98: standard toys.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Replicate: pop n, pop value → push value n times |
| `B` | Butterfly: pop b, pop a → push (a+b), (a-b) |
| `C` | Copy ascending: pop dest_v, size_v, src_v → copy 2D region |
| `D` | Decrement: pop a → push (a-1) |
| `E` | Sum all stack items; reflect if no IFungeStackContext |
| `F` | Fill space from stack: pop dest_v, m, n → write n*m cells |
| `G` | Get from space to stack: pop src_v, m, n → push n*m cells |
| `H` | Shift: pop b, pop a → if b>0: a<<b; if b<0: a>>(-b); else a |
| `I` | Increment: pop a → push (a+1) |
| `J` | Shift column; reflect if no IFungeSpaceContext/VectorContext/PositionContext |
| `K` | Copy descending: pop dest_v, size_v, src_v |
| `L` | Peek left (position-delta); reflect if no IFungeSpaceContext/PositionContext |
| `M` | Move ascending (copy+fill with spaces) |
| `N` | Negate: pop a → push (-a) |
| `O` | Shift row; reflect if no IFungeSpaceContext/VectorContext/PositionContext |
| `P` | Product all stack items; reflect if no IFungeStackContext |
| `Q` | Set cell at (position-delta); reflect if no IFungeSpaceContext/PositionContext |
| `R` | Peek right (position+delta); reflect if no IFungeSpaceContext/PositionContext |
| `S` | Fill space with value: pop dest_v, size_v, value |
| `T` | Set delta direction for dimension |
| `U` | Random direction; reflect if no IFungeRandomContext/PositionContext |
| `V` | Move descending |
| `W` | Wait: reflect unless cell@target==value |
| `X` | Increment Position.X; reflect if no IFungePositionContext |
| `Y` | Increment Position.Y; reflect if no IFungePositionContext |
| `Z` | Increment Position.Z; reflect if no IFungePositionContext |

## References

- [PyFunge TOYS fingerprint](https://pythonhosted.org/PyFunge/fingerprint/TOYS.html)
