# Esolang.Funge.Fingerprints.Ical

Provides the `ICAL` fingerprint (`0x4943414C`) for Funge-98: INTERCAL-like functions.

Per-IP address stack. Implements `IFungeInstructionPointerLifecycle` for clone/terminate.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Unary AND: a & ror(a,1) with bit-width determined by value magnitude |
| `O` | Unary OR: a \| ror(a,1) |
| `X` | Unary XOR: a ^ ror(a,1) |
| `I` | Mingle(a,b): interleave bits; result bit 2k+1=a bit k, bit 2k=b bit k |
| `S` | Select(a,b): collect bits of a where b's bit is set, right-justified |
| `N` | NEXT: push position, jump to target; reflect if stack>=79 or no IFungePositionContext/VectorContext |
| `R` | RESUME n: pop n entries from address stack, set position to last; reflect if n<0 |
| `F` | FORGET n: discard n entries from address stack; reflect if n<0 |

## References

- [PyFunge ICAL fingerprint](https://pythonhosted.org/PyFunge/fingerprint/ICAL.html)
