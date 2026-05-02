using Esolang.Funge.Interpreter;

namespace Esolang.Funge.Interpreter;

/// <summary>
/// Entry point for the dotnet-funge command-line tool.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs the command-line pipeline and returns the process exit code.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(string[] args)
    {
        var rootCommand = FungeInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync();
    }

    /// <summary>Application entry point.</summary>
    public static async Task<int> Main(string[] args)
        => await RunAsync(args);
}
