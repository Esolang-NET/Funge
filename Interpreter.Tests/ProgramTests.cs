namespace Esolang.Funge.Interpreter.Tests;

[TestClass]
public class ProgramTests(TestContext TestContext)
{
#pragma warning disable MSTEST0054
    CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054

    static int Run(string[] args)
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint;
        Assert.IsNotNull(entryPoint);
        object?[] parameters = [args];
        var result = entryPoint.Invoke(null, parameters) as int?;
        Assert.IsNotNull(result);
        return result.Value;
    }

    [TestMethod]
    public void Run_Default_ReturnsOne()
    {
        var exitCode = Run([]);
        Assert.AreEqual(1, exitCode);
    }

    [TestMethod]
    public void Run_HelpOption_ReturnsZero()
    {
        var exitCode = Run(["--help"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task Run_HelloWorld_ReturnsZero()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, "64+\"!dlroW ,olleH\">:#,_@", CancellationToken);
            var exitCode = Run(["--path", path]);
            Assert.AreEqual(0, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void Run_WithFingerprintBool_ReturnsZero()
    {
        const string source = "\"LOOB\"4(0N0AN1-q";
        var exitCode = Run(["--source", source, "--fingerprint-bool"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void Run_WithFingerprintArry_ReturnsZero()
    {
        const string source = "\"YRRA\"4(G3-q";
        var exitCode = Run(["--source", source, "--fingerprint-arry"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void Run_WithFingerprintDate_ReturnsZero()
    {
        const string source = "\"ETAD\"4(.@";
        var exitCode = Run(["--source", source, "--fingerprint-date"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public void Run_SourceOptionWithMultilineCode_ReturnsZero()
    {
        const string source = "v\n>25*\"!dlroW ,olleH\",,,,@";
        var exitCode = Run(["--source", source]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task Run_PathAndSourceTogether_ReturnsOne()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, "@", CancellationToken);
            var exitCode = Run(["--path", path, "--source", "@"]);
            Assert.AreEqual(1, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RunAsync_CancelledToken_StopsInfiniteProgram()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, ">", CancellationToken);

            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            var exitCode = await Program.RunAsync(["--path", path], cancellationToken: cancellation.Token);
            Assert.AreEqual(0, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
