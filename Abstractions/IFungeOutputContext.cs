namespace Esolang.Funge;

/// <summary>
/// Provides access to standard output for fingerprint instructions that need to write text.
/// </summary>
public interface IFungeOutputContext
{
    /// <summary>Writes text to the program's standard output.</summary>
    void WriteString(string value);
}
