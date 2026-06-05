namespace Esolang.Funge;

/// <summary>
/// Provides a fluent API for building the <see cref="IFingerprint.Instructions"/> dictionary.
/// </summary>
/// <example>
/// <code>
/// IReadOnlyDictionary&lt;char, FingerprintInstruction&gt; instructions =
///     new FingerprintBuilder()
///         .Add('A', static ctx =&gt; ctx.Push(ctx.Pop() + ctx.Pop()))
///         .Add('B', static ctx =&gt; ctx.Reflect())
///         .BuildInstructions();
/// </code>
/// </example>
public sealed class FingerprintBuilder
{
    readonly Dictionary<char, FingerprintInstruction> _instructions = [];

    /// <summary>Adds or replaces an instruction handler for the given letter.</summary>
    /// <param name="letter">An uppercase letter (<c>'A'</c>–<c>'Z'</c>).</param>
    /// <param name="instruction">The handler delegate.</param>
    /// <returns>This builder, for chaining.</returns>
    public FingerprintBuilder Add(char letter, FingerprintInstruction instruction)
    {
        _instructions[letter] = instruction;
        return this;
    }

    /// <summary>
    /// Returns the built instructions dictionary.
    /// The builder should not be modified after calling this method.
    /// </summary>
    public IReadOnlyDictionary<char, FingerprintInstruction> BuildInstructions() =>
        _instructions;
}
