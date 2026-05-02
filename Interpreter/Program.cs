using Esolang.Funge.Parser;
using Esolang.Funge.Processor;
using System.CommandLine;

var pathArgument = new Argument<string>("path")
{
    Description = "Path to a Funge-98 source file (.b98).",
};

var rootCommand = new RootCommand("Run Funge-98 (Befunge-98) programs.")
{
    pathArgument,
};

rootCommand.SetAction(parseResult =>
{
    var path = parseResult.GetValue(pathArgument)!;
    var space = FungeParser.ParseFile(path);
    var proc = new FungeProcessor(space, Console.Out, Console.In);
    return proc.Run();
});

return await rootCommand.Parse(args).InvokeAsync();
