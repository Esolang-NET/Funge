# Esolang.Funge.Fingerprints.Cshp

Provides the `CSHP` fingerprint (`0x43534850`) for Funge-98: evaluate C# expressions/scripts.

## Instructions

| Instruction | Description |
|---|---|
| `E` | `(0gnirts -- 0gnirts)` Evaluate C# code and push the result as `0gnirts` |
| `I` | `(0gnirts -- n)` Evaluate C# code and push the result converted to integer |
| `S` | `( -- n)` Push `0` to report the CSHP evaluator is available |

`E` / `I` reflect on evaluation failure.

## Notes

- `E` and `I` evaluate only the popped `0gnirts` script text; no implicit host arguments or injected variables are provided.
- The script text may contain multiple statements and an explicit `return` statement, as supported by Roslyn C# scripting.
- `E` converts the evaluation result with `.ToString()` (`null` becomes empty string).
- `I` accepts native integer results and values convertible to `int`.

## References

- [RC/Funge mirror (PERL E/I/S behavior)](https://www.club.cc.cmu.edu/~ajo/funge/rcfunge-mirror.txt)
