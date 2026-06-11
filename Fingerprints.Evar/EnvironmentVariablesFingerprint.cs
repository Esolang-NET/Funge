using System.Collections;

namespace Esolang.Funge.Fingerprints.Evar;

/// <summary>
/// Provides the standard Funge-98 <c>EVAR</c> fingerprint (handprint <c>0x45564152</c>).
/// </summary>
public sealed class EnvironmentVariablesFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("EVAR");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="EnvironmentVariablesFingerprint"/>.</summary>
    public EnvironmentVariablesFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('G', GetVariable)
            .Add('N', CountVariables)
            .Add('P', PutVariable)
            .Add('V', GetVariableByIndex)
            .BuildInstructions();

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var chars = new System.Collections.Generic.List<char>();
        int c;
        while ((c = ctx.Pop()) != 0)
            chars.Insert(0, (char)c);
        return new string([.. chars]);
    }

    static void Push0gnirts(IFungeExecutionContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    static void GetVariable(IFungeExecutionContext ctx)
    {
        var name = Pop0gnirts(ctx);
        var value = Environment.GetEnvironmentVariable(name) ?? string.Empty;
        Push0gnirts(ctx, value);
    }

    static void CountVariables(IFungeExecutionContext ctx)
        => ctx.Push(Environment.GetEnvironmentVariables().Count);

    static void PutVariable(IFungeExecutionContext ctx)
    {
        var nameValue = Pop0gnirts(ctx);
        var eqIndex = nameValue.IndexOf('=');
        if (eqIndex < 0)
        {
            ctx.Reflect();
            return;
        }

        var name = nameValue[..eqIndex];
        var value = nameValue[(eqIndex + 1)..];
        Environment.SetEnvironmentVariable(name, value);
    }

    static void GetVariableByIndex(IFungeExecutionContext ctx)
    {
        var i = ctx.Pop();
        var envVars = Environment.GetEnvironmentVariables();
        var sorted = new System.Collections.Generic.List<string>();
        foreach (DictionaryEntry entry in envVars)
            sorted.Add($"{entry.Key}={entry.Value}");
        sorted.Sort(StringComparer.Ordinal);

        if (i < 0 || i >= sorted.Count)
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, sorted[i]);
    }
}
