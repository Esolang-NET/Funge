namespace Esolang.Funge.Interpreter.Tests;

public class ProgramTests
{

    static int Run(string[] args)
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint;
        Assert.NotNull(entryPoint);
        object?[] parameters = [args];
        var result = entryPoint.Invoke(null, parameters) as int?;
        Assert.NotNull(result);
        return result.Value;
    }

    [Test]
    public async Task Run_Default_ReturnsOne()
    {
        var exitCode = Run([]);
        await Assert.That(exitCode).IsEqualTo(1);
    }

    [Test]
    public async Task Run_HelpOption_ReturnsZero()
    {
        var exitCode = Run(["--help"]);
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_HelloWorld_ReturnsZero(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, "64+\"!dlroW ,olleH\">:#,_@", CancellationToken);
            var exitCode = Run(["--path", path]);
            await Assert.That(exitCode).IsEqualTo(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Run_WithFingerprintBool_ReturnsZero()
    {
        const string source = "\"LOOB\"4(0N0AN1-q";
        var exitCode = Run(["--source", source, "--fingerprint-bool"]);
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_WithFingerprintArry_ReturnsZero()
    {
        const string source = "\"YRRA\"4(G3-q";
        var exitCode = Run(["--source", source, "--fingerprint-arry"]);
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_WithFingerprintDate_ReturnsZero()
    {
        const string source = "\"ETAD\"4(.@";
        var exitCode = Run(["--source", source, "--fingerprint-date"]);
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_SourceOptionWithMultilineCode_ReturnsZero()
    {
        const string source = "v\n>25*\"!dlroW ,olleH\",,,,@";
        var exitCode = Run(["--source", source]);
        await Assert.That(exitCode).IsEqualTo(0);
    }

    [Test]
    public async Task Run_PathAndSourceTogether_ReturnsOne(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, "@", CancellationToken);
            var exitCode = Run(["--path", path, "--source", "@"]);
            await Assert.That(exitCode).IsEqualTo(1);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task RunAsync_CancelledToken_StopsInfiniteProgram(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, ">", CancellationToken);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var exitCode = await Program.RunAsync(["--path", path], cancellationToken: cancellation.Token);
            await Assert.That(exitCode).IsEqualTo(0);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
