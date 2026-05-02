# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Added

- Initial implementation of Funge-98 (Befunge-98) parser, processor and interpreter.
- `Esolang.Funge.Parser`: FungeSpace (sparse infinite 2D grid), FungeVector, FungeParser.
- `Esolang.Funge.Processor`: FungeProcessor supporting core Funge-98 instruction set, stack stack, concurrent IPs.
- `dotnet-funge`: Command-line interpreter for `.b98` files.
