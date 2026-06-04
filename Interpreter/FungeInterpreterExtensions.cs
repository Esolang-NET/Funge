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
        var pathOption = new Option<string?>(name: "--path", aliases: ["-p"])
        {
            Description = "Path to a Funge-98 source file (.b98).",
        };

        var sourceOption = new Option<string?>(name: "--source", aliases: ["-s"])
        {
            Description = "Inline Funge-98 source code. Newlines are supported.",
        };

        var fingerprintNullOption = new Option<bool>(name: "--fingerprint-null")
        {
            Description = "Enable the NULL fingerprint (0x4E554C4C): all 26 instructions reflect.",
        };

        var fingerprintFileOption = new Option<bool>(name: "--fingerprint-file")
        {
            Description = "Enable the FILE fingerprint (0x46494C45): file I/O instructions C D G M O P R S W.",
        };

        var rootCommand = new RootCommand("Run Funge-98 (Befunge-98) programs.")
        {
            pathOption,
            sourceOption,
            fingerprintNullOption,
            fingerprintFileOption,
        };

        rootCommand.SetAction(async (parseResult, cancellationToken) =>
        {
            var path = parseResult.GetValue(pathOption);
            var source = parseResult.GetValue(sourceOption);
            var useNull = parseResult.GetValue(fingerprintNullOption);
            var useFile = parseResult.GetValue(fingerprintFileOption);

            var hasPath = !string.IsNullOrWhiteSpace(path);
            var hasSource = source is not null;
            if (hasPath == hasSource)
            {
                Console.Error.WriteLine("Specify exactly one of --path/-p or --source/-s.");
                return 1;
            }

            var space = hasPath
                ? FungeParser.ParseFile(path!)
                : FungeParser.Parse(source!);
            var env = Environment.GetEnvironmentVariables()
                .Cast<DictionaryEntry>()
                .Select(static entry => $"{entry.Key}={entry.Value}");
            var inputArgument = hasPath ? path! : "<inline-source>";

            using var fileFingerprint = useFile ? new FileFingerprint() : null;
            List<IFingerprint> fingerprints = [];
            if (useNull) fingerprints.Add(NullFingerprint.Instance);
            if (useFile) fingerprints.Add(fileFingerprint!);

            var proc = new FungeProcessor(
                space,
                commandLineArguments: [inputArgument],
                environmentVariables: env,
                fingerprints: fingerprints.Count > 0 ? fingerprints : null);

            return await proc.RunToConsoleAsync(cancellationToken);
        });

        return rootCommand;
    }
}
