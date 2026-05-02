using Esolang.Funge.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Interpreter.Tests;

[TestClass]
public class ProgramTests
{
    const string HelloWorldProgram = "64+\"!dlroW ,olleH\">:#,_@";

    [TestMethod]
    public async Task RunAsync_HelpOption_ReturnsZero()
    {
        var exitCode = await Program.RunAsync(["--help"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_HelloWorld_ReturnsZero()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.b98");
        try
        {
            await File.WriteAllTextAsync(path, HelloWorldProgram);

            var exitCode = await Program.RunAsync([path]);
            Assert.AreEqual(0, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
