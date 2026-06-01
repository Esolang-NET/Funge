using Esolang.Funge.Parser;
using Esolang.Funge.Processor;
using Esolang.Processor;
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

            var exitCode = 0;
            await foreach (var ioEvent in proc.RunAsyncEnumerable(cancellationToken))
            {
                switch (ioEvent)
                {
                    case InputCharEvent ice:
                        {
                            var c = Console.In.Read();
                            if (c != -1) ice.Write((char)c);
                        }
                        break;
                    case InputIntEvent iie:
                        {
                            var line = await Console.In.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                            if (int.TryParse(line, out var val)) iie.Write(val);
                        }
                        break;
                    case OutputCharEvent oce:
                        Console.Out.Write(oce.Output);
                        break;
                    case OutputIntEvent oie:
                        Console.Out.Write(oie.Output);
                        break;
                    case EndEvent ee:
                        exitCode = ee.ExitCode;
                        break;
                }
            }
            return exitCode;
        });

        return rootCommand;
    }
}
