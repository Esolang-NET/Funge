using Esolang.Funge.Interpreter;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

return await RunWithCancelRegisterAsync(args);

/// <summary>
/// Entry point for the dotnet-funge command-line tool.
/// </summary>
partial class Program
{
    /// <summary>
    /// Runs the dotnet-funge interpreter with the specified command-line arguments. This method initializes the command-line parser, adds the Funge commands, and invokes the appropriate command based on the provided arguments. It also supports cancellation through a CancellationToken, allowing for graceful termination of ongoing operations if requested.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public static async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return 0;
        return await new RootCommand()
            .AddFungeCommands()
            .Parse(args)
            .InvokeAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Runs the dotnet-brainfuck interpreter with the specified command-line arguments, input reader, and output writer. This method allows for redirection of input and output streams, making it suitable for testing or integration scenarios. It also supports cancellation through a CancellationToken.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="reader">The input reader.</param>
    /// <param name="writer">The output writer.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The exit code of the interpreter.</returns>
    [ExcludeFromCodeCoverage]
    public static async Task<int> RunAsync(string[] args, TextReader? reader = null, TextWriter? writer = null, CancellationToken cancellationToken = default)
    {
        TextReader? originalReader = null;
        TextWriter? originalWriter = null;
        if (reader is not null)
        {
            originalReader = Console.In;
            Console.SetIn(reader);
        }
        if (writer is not null)
        {
            originalWriter = Console.Out;
            Console.SetOut(writer);
        }
        try
        {
            return await RunAsync(args, cancellationToken);
        }
        finally
        {
            if (originalReader is not null)
                Console.SetIn(originalReader);
            if (originalWriter is not null)
                Console.SetOut(originalWriter);
        }
    }

    /// <summary>
    /// Runs the dotnet-funge command-line tool with cancellation support. This method sets up a cancellation token that listens for user interruptions (e.g., Ctrl+C) and gracefully handles the cancellation of ongoing operations. It ensures that the application can terminate cleanly without leaving resources in an inconsistent state.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    [ExcludeFromCodeCoverage]
    static async Task<int> RunWithCancelRegisterAsync(string[] args)
    {
        try
        {
            Console.CancelKeyPress += CancelKeyPress;
            return await RunAsync(args, cancellationTokenSource.Token);
        }
        finally
        {
            Console.CancelKeyPress -= CancelKeyPress;
        }
    }

    /// <summary>
    /// A static readonly CancellationTokenSource used to signal cancellation of the interpreter's operations. This token source is shared across the application and is used to handle user interruptions, such as pressing Ctrl+C, allowing for graceful termination of ongoing tasks.
    /// </summary>
    static readonly CancellationTokenSource cancellationTokenSource = new();

    /// <summary>
    /// Handles the Console.CancelKeyPress event to allow graceful cancellation of the interpreter when the user presses Ctrl+C. This method sets the Cancel property of the event arguments to true, preventing the application from terminating immediately, and signals the cancellation token source to cancel any ongoing operations.
    /// </summary>
    /// <param name="_">The sender of the event.</param>
    /// <param name="e">The event arguments containing information about the cancel key press.</param>
    [ExcludeFromCodeCoverage]
    static void CancelKeyPress(object? _, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;
        cancellationTokenSource.Cancel();
    }
}
