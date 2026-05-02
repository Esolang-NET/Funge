using Microsoft.CodeAnalysis;

namespace Esolang.Funge.Generator;

/// <summary>
/// Provides diagnostic definitions reported during source generation.
/// </summary>
public static class DiagnosticDescriptors
{
    const string Category = "Funge";

    /// <summary>
    /// FG0001: Invalid source path parameter.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidSourcePathParameter = new DiagnosticDescriptor(
        id: "FG0001",
        title: "Invalid source path parameter",
        messageFormat: "The source path parameter of the attribute on the method '{0}' must not be null or empty",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0002: Unsupported return type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidReturnType = new DiagnosticDescriptor(
        id: "FG0002",
        title: "Unsupported return type",
        messageFormat: "The method return type '{0}' is not supported for Funge code generation",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0003: Unsupported parameter type.
    /// </summary>
    public static readonly DiagnosticDescriptor InvalidParameter = new DiagnosticDescriptor(
        id: "FG0003",
        title: "Unsupported parameter type",
        messageFormat: "The parameter '{0}' of the method has an unsupported type",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0004: Source file not found.
    /// </summary>
    public static readonly DiagnosticDescriptor SourceFileNotFound = new DiagnosticDescriptor(
        id: "FG0004",
        title: "Funge source file not found",
        messageFormat: "The Funge source file '{0}' could not be found in AdditionalFiles",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0005: Consumer language version is below C# 8.0.
    /// </summary>
    public static readonly DiagnosticDescriptor LanguageVersionTooLow = new DiagnosticDescriptor(
        id: "FG0005",
        title: "Language version too low",
        messageFormat: "Funge source generation requires C# 8.0 or later (current: {0})",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0006: Duplicate parameter type.
    /// </summary>
    public static readonly DiagnosticDescriptor DuplicateParameter = new DiagnosticDescriptor(
        id: "FG0006",
        title: "Duplicate parameter type",
        messageFormat: "The parameter type '{0}' appears more than once in method '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0007: Return type and output parameter conflict.
    /// </summary>
    public static readonly DiagnosticDescriptor ReturnOutputConflict = new DiagnosticDescriptor(
        id: "FG0007",
        title: "Return type and output parameter conflict",
        messageFormat: "Method '{0}' has both a non-void return type and an output parameter (TextWriter/PipeWriter); use one or the other",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0008: Output interface required.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredOutputInterface = new DiagnosticDescriptor(
        id: "FG0008",
        title: "Output interface required",
        messageFormat: "Method '{0}' uses Funge output instructions but has no output (return string/IEnumerable&lt;byte&gt; or a TextWriter/PipeWriter parameter)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0009: Input interface required.
    /// </summary>
    public static readonly DiagnosticDescriptor RequiredInputInterface = new DiagnosticDescriptor(
        id: "FG0009",
        title: "Input interface required",
        messageFormat: "Method '{0}' uses Funge input instructions but has no input (string, TextReader, or PipeReader parameter)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    /// <summary>
    /// FG0010: Unused input interface.
    /// </summary>
    public static readonly DiagnosticDescriptor UnusedInputInterface = new DiagnosticDescriptor(
        id: "FG0010",
        title: "Unused input interface",
        messageFormat: "Method '{0}' has an input parameter but the Funge source does not use any input instructions",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Hidden,
        isEnabledByDefault: true);
}
