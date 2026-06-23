namespace Esolang.Funge;

/// <summary>
/// Provides access to standard output for fingerprint instructions that need to write text.
/// </summary>
public interface IFungeOutputContext
{
    /// <summary>Writes text to the program's standard output.</summary>
    /// <param name="value">The text to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteStringAsync(string value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    Task WriteLineAsync(string value);

    /// <summary>
    /// Writes a single character to the program's standard output.
    /// </summary>
    /// <param name="value">The character to write.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    Task WriteCharAsync(char value);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    Task WriteIntAsync(int value);
}
