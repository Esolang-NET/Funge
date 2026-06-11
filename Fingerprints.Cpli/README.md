# Esolang.Funge.Fingerprints.Cpli

Provides the `CPLI` fingerprint (`0x43504C49`) for Funge-98: complex integer extension.

Stack layout: real (lower on stack), imaginary (upper/top). For binary ops: pop imag2, real2, imag1, real1.

## Instructions

| Instruction | Description |
|---|---|
| `A` | Add: (a+bi)+(c+di) → push (a+c), (b+d) |
| `D` | Divide (truncated): (a+bi)/(c+di); reflect if c==0 and d==0 |
| `M` | Multiply: (a+bi)*(c+di) → push (ac-bd), (ad+bc) |
| `O` | Output: pop b(imag), pop a(real) → print "a+bi"; reflect if no IFungeOutputContext |
| `S` | Subtract: (a+bi)-(c+di) → push (a-c), (b-d) |
| `V` | Magnitude: pop b, pop a → push (int)sqrt(a²+b²) |

## References

- [PyFunge CPLI fingerprint](https://pythonhosted.org/PyFunge/fingerprint/CPLI.html)
