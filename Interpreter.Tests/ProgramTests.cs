using Esolang.Funge.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Interpreter.Tests;

[TestClass]
public class ProgramTests(TestContext TestContext)
{
#pragma warning disable MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
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
    public void Run_Default_ReturnsZero()
    {
        var exitCode = Run([]);
        Assert.AreEqual(0, exitCode);
    }
    const string HelloWorldProgram = "64+\"!dlroW ,olleH\">:#,_@";

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
            await File.WriteAllTextAsync(path, HelloWorldProgram, CancellationToken);

            var exitCode = Run([path]);
            Assert.AreEqual(0, exitCode);
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

            var exitCode = await Program.RunAsync([path], cancellationToken: cancellation.Token);
            Assert.AreEqual(0, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
