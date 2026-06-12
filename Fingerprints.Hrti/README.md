# Esolang.Funge.Fingerprints.Hrti

Provides the `HRTI` fingerprint (`0x48525449`) for Funge-98: high-resolution timer interface.

Per-IP marks using `IFungeInstructionPointerContext`. Implements `IFungeInstructionPointerLifecycle` for clone/terminate.

## Instructions

| Instruction | Description |
|---|---|
| `E` | Erase mark for current IP |
| `G` | Push granularity in microseconds (1_000_000 / Stopwatch.Frequency) |
| `M` | Record current timestamp for current IP |
| `S` | Push microseconds since last whole second |
| `T` | Push microseconds since last mark; reflect if no mark |

## References

- [PyFunge HRTI fingerprint](https://pythonhosted.org/PyFunge/fingerprint/HRTI.html)
