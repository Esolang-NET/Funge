# Esolang.Funge.Fingerprints.Jstr

Provides the `JSTR` fingerprint (`0x4A535452`) for Funge-98: Jesse van Herk's string extensions.

Requires `IFungeSpaceContext` and `IFungeVectorContext`; reflect if unavailable.

## Instructions

Stack notation (bottom to top):

| Instruction | Stack | Description |
|---|---|---|
| `G` | `delta_v pos_v n —` str_s | Pop n, pop pos_v, pop delta_v → read n chars from space; reflect if n<0 |
| `P` | `str_s delta_v pos_v n —` | Pop n, pop pos_v, pop delta_v, pop str → write min(n, str.Len) chars; reflect if n<0 |

## References

- [PyFunge JSTR fingerprint](https://pythonhosted.org/PyFunge/fingerprint/JSTR.html)
