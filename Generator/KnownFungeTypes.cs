using Esolang.Generator;
using Microsoft.CodeAnalysis;

namespace Esolang.Funge.Generator;

readonly struct KnownFungeTypes(Compilation compilation)
{
    public INamedTypeSymbol? IFingerprint { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFingerprint");
    public INamedTypeSymbol? FingerprintInstruction { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.FingerprintInstruction");
    public INamedTypeSymbol? IFungeExecutionContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeExecutionContext");
    public INamedTypeSymbol? IFungeInstructionPointerContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeInstructionPointerContext");
    public INamedTypeSymbol? IFungeInstructionPointerLifecycle { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeInstructionPointerLifecycle");
    public INamedTypeSymbol? IFungeInputContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeInputContext");
    public INamedTypeSymbol? IFungeOutputContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeOutputContext");
    public INamedTypeSymbol? IFungeVectorContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeVectorContext");
    public INamedTypeSymbol? IFungeSpaceContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeSpaceContext");
    public INamedTypeSymbol? IFungeStorageOffsetContext { get; } = compilation.GetBestTypeByMetadataName("Esolang.Funge.IFungeStorageOffsetContext");
}
