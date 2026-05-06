# Esolang.Funge.Parser

Core parsing primitives for [Funge-98](https://github.com/catseye/Funge-98/blob/master/doc/funge98.markdown) programs.

## Overview

This library provides the fundamental data structures for representing and manipulating a Funge-98 program space.

| Type | Description |
|---|---|
| `FungeVector` | Immutable 2D coordinate `(X, Y)` |
| `FungeSpace` | Sparse infinite 2D grid of integer cells (space = 32) |
| `FungeParser` | Parses Funge-98 source text into a `FungeSpace` |

## Installation

```
dotnet add package Esolang.Funge.Parser
```

## Usage

```csharp
using Esolang.Funge.Parser;

// Parse from a string
FungeSpace space = FungeParser.Parse("64+\"!dlroW ,olleH\">:#,_@");

// Parse from a file
FungeSpace space = FungeParser.ParseFile("hello.b98");

// Read / write cells
int value = space[new FungeVector(0, 0)];
space[new FungeVector(0, 0)] = 'A';
```

## Target Frameworks

`netstandard2.0` · `net8.0` · `net9.0` · `net10.0`

AOT / trimming compatible.
