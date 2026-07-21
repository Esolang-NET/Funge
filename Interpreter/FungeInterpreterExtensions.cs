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
    public static T AddFungeCommands<T>(this T rootCommand)
        where T : Command
    {
        var pathOption = new Option<string?>(name: "--path", aliases: ["-p"])
        {
            Description = "Path to a Funge-98 source file (.b98).",
        };

        var sourceOption = new Option<string?>(name: "--source", aliases: ["-s"])
        {
            Description = "Inline Funge-98 source code. Newlines are supported.",
        };
        var fingerprintsOptions = new FingerprintsOptions();

        rootCommand.Description = "Run Funge-98 (Befunge-98) programs.";
        rootCommand.Add(pathOption);
        rootCommand.Add(sourceOption);
        foreach (var option in fingerprintsOptions.Options)
            rootCommand.Add(option);

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var source = parseResult.GetValue(sourceOption);
            var fingerprints = fingerprintsOptions.GetValues(parseResult);
            try
            {
                var hasPath = !string.IsNullOrWhiteSpace(path);
                var hasSource = source is not null;
                if (hasPath == hasSource)
                {
                    Console.Error.WriteLine("Specify exactly one of --path/-p or --source/-s.");
                    return 1;
                }

                var space = hasPath
                    ? FungeParser.ParseFile(path!, cancellationToken)
                    : FungeParser.Parse(source!, cancellationToken);
                var env = Environment.GetEnvironmentVariables()
                    .Cast<DictionaryEntry>()
                    .Select(static entry => $"{entry.Key}={entry.Value}");
                var inputArgument = hasPath ? path! : "<inline-source>";


                var proc = new FungeProcessor(
                    space,
                    commandLineArguments: [inputArgument],
                    environmentVariables: env,
                    fingerprints: fingerprints);

                return await proc.RunToConsoleAsync(cancellationToken);
            }
            finally
            {
                foreach (var fingerprint in fingerprints ?? [])
                {
                    if (fingerprint is IAsyncDisposable asyncDisposable)
                        await asyncDisposable.DisposeAsync();
                    else if (fingerprint is IDisposable disposable)
                        disposable.Dispose();
                }
            }
        });

        return rootCommand;
    }
}
