using Esolang.Funge.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Interpreter.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public async Task RunAsync_HelpOption_ReturnsZero()
    {
        var exitCode = await Program.RunAsync(["--help"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_HelloWorld_ReturnsZero()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Programs", "hello.b98");
        var exitCode = await Program.RunAsync([path]);
        Assert.AreEqual(0, exitCode);
    }
}
