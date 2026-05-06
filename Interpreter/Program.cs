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
    /// <param name="cancellationToken">Token to cancel command execution.</param>
    /// <returns>The exit code.</returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        var rootCommand = FungeInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
    }

    /// <summary>Application entry point.</summary>
    public static async Task<int> Main(string[] args)
    {
        using var cancellation = new CancellationTokenSource();
        void OnCancelKeyPress(object? _, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            cancellation.Cancel();
        }

        Console.CancelKeyPress += OnCancelKeyPress;
        try
        {
            return await RunAsync(args, cancellation.Token);
        }
        finally
        {
            Console.CancelKeyPress -= OnCancelKeyPress;
        }
    }
}
