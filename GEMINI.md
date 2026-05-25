# Development Guide

## Testing

This project uses the Microsoft Testing Platform (MTP).

### Running Tests

To run all tests in the solution:

```bash
dotnet test
```

### Collecting Code Coverage

To run tests and collect code coverage:

```bash
dotnet test --coverage --coverage-output-format cobertura
```

To generate an HTML coverage report using ReportGenerator:

```bash
dotnet reportgenerator "-reports:**/*.cobertura.xml" "-targetdir:coveragereport" -reporttypes:Html
```
The report will be generated in the `coveragereport` directory.

## Logging Architecture

This project implements conditional logging in generated code. 

- **Mechanism**: The `MethodGenerator` inspects the target class for an `ILogger` (or `ILogger<T>`) field.
- **Pattern**: If detected, it injects zero-allocation logging code using `LoggerMessage.Define` inside the generated method body.
- **Compatibility**: To ensure backward compatibility and avoid hard dependencies in the generated `FungeRuntime` facade, facade methods maintain their original signature. Internal runtime methods accept `object? logger` which is conditionally cast and used only if available.
