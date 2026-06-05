namespace Esolang.Funge;

/// <summary>
/// Provides access to standard input for fingerprint instructions that need to read text.
/// </summary>
public interface IFungeInputContext
{
    /// <summary>Reads one line from the program's standard input, or <c>null</c> on EOF.</summary>
    string? ReadLine();
}
