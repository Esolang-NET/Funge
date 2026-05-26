using Esolang.Funge.Interpreter;

using var cancellation = new CancellationTokenSource();
void OnCancelKeyPress(object? _, ConsoleCancelEventArgs e)
{
    e.Cancel = true;
    cancellation.Cancel();
}

Console.CancelKeyPress += OnCancelKeyPress;
try
{
    return await Program.RunAsync(args, cancellation.Token);
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

namespace Esolang.Funge.Interpreter;

/// <summary>
/// Entry point for the dotnet-funge command-line tool.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Runs the command-line pipeline and returns the process exit code.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var rootCommand = FungeInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
    }
}
