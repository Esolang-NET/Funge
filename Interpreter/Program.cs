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
    return await RunAsync(args, cancellation.Token);
}
finally
{
    Console.CancelKeyPress -= OnCancelKeyPress;
}

/// <summary>
/// Entry point for the dotnet-funge command-line tool.
/// </summary>
partial class Program
{
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return 0;

        var rootCommand = FungeInterpreterExtensions.BuildRootCommand();
        return await rootCommand.Parse(args).InvokeAsync(cancellationToken: cancellationToken);
    }
}
