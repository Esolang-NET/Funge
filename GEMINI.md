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
