using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Esolang.Funge.Interpreter;

/// <summary>
/// 
/// </summary>
interface IOptionAndMakeFingerprintPair : IEnumerable<Option>
{

    bool TryGetValue(ParseResult parseResult, [NotNullWhen(true)] out IFingerprint? fingerprint);
}
