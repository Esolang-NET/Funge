using Esolang.Funge.Parser;
using Esolang.Funge.Processor;
using Esolang.Interpreter;
using System.Collections;
using System.CommandLine;

namespace Esolang.Funge.Interpreter;

/// <summary>
/// Extension methods that compose the dotnet-funge CLI commands.
/// </summary>
public static class FungeInterpreterExtensions
{
    /// <summary>
    /// Builds and returns the root command for the dotnet-funge tool.
    /// </summary>
    public static RootCommand BuildRootCommand()
    {
        var pathArgument = new Argument<string>("path")
        {
            Description = "Path to a Funge-98 source file (.b98).",
        };

        var rootCommand = new RootCommand("Run Funge-98 (Befunge-98) programs.")
        {
            pathArgument,
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathArgument)!;
            var space = FungeParser.ParseFile(path);
            var env = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Select(static entry => $"{entry.Key}={entry.Value}");
            var proc = new FungeProcessor(
                space,
                commandLineArguments: [path],
                environmentVariables: env);

            return await proc.RunToConsoleAsync(cancellationToken);
        });

        return rootCommand;
    }
}
