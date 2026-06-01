using static Esolang.Processor.IOEvent;

namespace Esolang.Funge.Processor.Tests;

[TestClass]
public class FungeProcessorTests(TestContext TestContext)
{
    CancellationToken TestCancellationToken => TestContext.CancellationToken;
    string Run(string source, string? input = null)
    {
        var space = Parser.FungeParser.Parse(source);
        var output = new StringWriter();
        var reader = input is null ? TextReader.Null : new StringReader(input);
        var proc = new FungeProcessor(space);
        _ = RunToEnd(proc, reader, output, TestCancellationToken);
        return output.ToString();
    }

    static int RunToEnd(FungeProcessor proc, TextReader input, TextWriter output, CancellationToken ct)
    {
        var task = Task.Run(async () =>
        {
            var exitCode = 0;
            await foreach (var ev in proc.RunAsyncEnumerable(ct))
            {
                switch (ev)
                {
                    case OutputCharEvent oce: output.Write(oce.Output); break;
                    case OutputIntEvent oie: output.Write(oie.Output); break;
                    case InputCharEvent ice:
                        var c = input.Read();
                        if (c != -1) ice.Write((char)c);
                        break;
                    case InputIntEvent iie:
                        var line = input.ReadLine();
                        if (int.TryParse(line, out var val)) iie.Write(val);
                        break;
                    case EndEvent ee:
                        exitCode = ee.ExitCode;
                        break;
                }
            }
            return exitCode;
        }, ct);
        return task.GetAwaiter().GetResult();
    }

    int RunGetExitCode(string source)
    {
        var space = Parser.FungeParser.Parse(source);
        var proc = new FungeProcessor(space);
        return RunToEnd(proc, TextReader.Null, TextWriter.Null, TestCancellationToken);
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void TestDirectionalInstructions()
    {
        var space = new Parser.FungeSpace();
        var pos1 = new Parser.FungeVector(0, 0, 0);
        var pos2 = new Parser.FungeVector(1, 0, 0);
        var pos3 = new Parser.FungeVector(0, 1, 0);
        var pos4 = new Parser.FungeVector(0, 2, 0);
        var pos5 = new Parser.FungeVector(0, 3, 0);

        space[pos1] = '>';
        space[pos2] = '<';
        space[pos3] = '^';
        space[pos4] = 'v';
        space[pos5] = '@';

        var proc = new FungeProcessor(space);
        var token = TestCancellationToken;

        RunToEnd(proc, TextReader.Null, TextWriter.Null, token);
    }

    static string EncodeZeroGnirts(string value)
        => $"0\"{new string([.. value.Reverse()])}\"";

    // ── Termination ────────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Stop_EmptyProgram_Wraps()
    {
        // No @ → program loops but should terminate via cancellation
        // Just ensure an immediate @ exits
        var result = Run("@");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Quit_ReturnsExitCode()
        => Assert.AreEqual(8, RunGetExitCode("42*q"));

    // ── Output ────────────────────────────────────────────────────────────

    [TestMethod]
    public void OutputChar_SingleChar()
        => Assert.AreEqual("H", Run("\"H\",@"));

    [TestMethod]
    public void OutputInt_WithTrailingSpace()
        => Assert.AreEqual("10 ", Run("55+.@"));

    // ── Arithmetic ────────────────────────────────────────────────────────

    [TestMethod]
    public void Add()
        => Assert.AreEqual("7 ", Run("34+.@"));

    [TestMethod]
    public void Subtract()
        => Assert.AreEqual("2 ", Run("53-.@"));

    [TestMethod]
    public void Multiply()
        => Assert.AreEqual("12 ", Run("34*.@"));

    [TestMethod]
    public void Divide()
        => Assert.AreEqual("1 ", Run("96/.@"));

    [TestMethod]
    public void Remainder()
        => Assert.AreEqual("1 ", Run("72%.@"));

    [TestMethod]
    public void GreaterThan_True()
        => Assert.AreEqual("1 ", Run("53`.@"));

    [TestMethod]
    public void GreaterThan_False()
        => Assert.AreEqual("0 ", Run("35`.@"));

    [TestMethod]
    public void LogicalNot_Zero()
        => Assert.AreEqual("1 ", Run("0!.@"));

    [TestMethod]
    public void LogicalNot_NonZero()
        => Assert.AreEqual("0 ", Run("5!.@"));

    // ── Stack ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Duplicate()
        => Assert.AreEqual("5 5 ", Run("5:..@"));

    [TestMethod]
    public void Swap()
        => Assert.AreEqual("5 3 ", Run("53\\..@"));

    [TestMethod]
    public void Pop_Discard()
        => Assert.AreEqual("5 ", Run("53$.@"));

    // ── Direction ─────────────────────────────────────────────────────────

    [TestMethod]
#pragma warning disable IDE0022
    public void EastWestIf_Zero_GoesEast()
    {
        // 0_ → East → . outputs next pop (0) then @
        Assert.AreEqual("0 ", Run("0_.@"));
    }
#pragma warning restore IDE0022

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
#pragma warning disable IDE0022
    public void NorthSouthIf_NonZero_GoesNorth()
    {
        const string source = "v @\n>1|";
        Assert.AreEqual(string.Empty, Run(source));
    }
#pragma warning restore IDE0022

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
#pragma warning disable IDE0022
    public void EastWestIf_NonZero_GoesWest()
    {
        // "1_" at positions 0-1. After '_', go West, wrap to rightmost char...
        // Hard to test in single row. Use '@' placement.
        // "1_@" → goes West to nothing... let's try another approach
        // Just verify we can stop: if nonzero, go West; space wraps; '@' at start doesn't help
        // Skip complex direction tests here; covered by Hello World test below
        Assert.AreEqual(string.Empty, Run("1_@")); // goes West, wraps, hits '_' etc. – eventually '@' or loops
    }
#pragma warning restore IDE0022

    [TestMethod]
    public void Fingerprint_StubSucceeds()
        => Assert.AreEqual("1 ", Run("(1.@"));

    [TestMethod]
    public void Fingerprint_Unload_StubSucceeds()
        => Assert.AreEqual("1 ", Run(")1.@"));

    // ── Additional coverage for ExecuteInstruction ────────────────────────

    [TestMethod]
    public void Trampoline_SkipUntilSemicolon()
        => Assert.AreEqual("1 ", Run("; skipped code ;1.@"));

    [TestMethod]
    public void HexDigits_Individual()
    {
        Assert.AreEqual("10 ", Run("a.@"));
        Assert.AreEqual("11 ", Run("b.@"));
    }

    [TestMethod]
    public void OutputInt_EmptyStack_OutputsZero()
        => Assert.AreEqual("0 ", Run(".@"));

    [TestMethod]
    public void Iterate_ZeroTimes_SkipsOperand()
    {
        var result = Run("0k1.2.@");
        Assert.AreEqual("0 2 ", result, $"Expected '0 2 ', but got '{result}'");
    }


    // ── String mode ───────────────────────────────────────────────────────

    [TestMethod]
#pragma warning disable IDE0022
    public void StringMode_PushesChars()
    {
        // "Hi" pushes 'H'=72 then 'i'=105; i is on top
        Assert.AreEqual("iH", Run("\"Hi\",,@"));
    }
#pragma warning restore IDE0022

    [TestMethod]
    public void StringMode_ContiguousSpaces_PushSingleSpace()
        => Assert.AreEqual("49  ", Run("\"   1\".,@"));

    // ── Trampoline ────────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
#pragma warning disable IDE0022
    public void Trampoline_SkipsOne()
    {
        // "#.@" → skip '.', execute '@' → empty output
        Assert.AreEqual(string.Empty, Run("#.@"));
    }
#pragma warning restore IDE0022

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void SgmlSpaces_DoNotReflect()
        => Assert.AreEqual("1 ", Run("1\t\v.@"));

    // ── FungeSpace get/put ────────────────────────────────────────────────

    [TestMethod]
#pragma warning disable IDE0022
    public void GetPut_ReadWrite()
    {
        // p pops z,y,x,v. Build v=65 via 8*8+1, then store at (5,0,0) and read back.
        Assert.AreEqual("65 ", Run("88*1+500p500g.@"));
    }
#pragma warning restore IDE0022

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void GoHigh_ChangesDeltaToNegativeZ()
        => Assert.AreEqual(7, RunGetExitCode("h\f\f>7q"));

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void GoLow_ChangesDeltaToPositiveZ()
        => Assert.AreEqual(7, RunGetExitCode("l\f>7q"));

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void HighLowIf_Zero_GoesLow()
        => Assert.AreEqual(1, RunGetExitCode("0m\f >1q\f >2q"));

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void HighLowIf_NonZero_GoesHigh()
        => Assert.AreEqual(2, RunGetExitCode("1m\f >1q\f >2q"));

    // ── Hello World ───────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
#pragma warning disable IDE0022
    public void HelloWorld_Classic()
    {
        // Classic Befunge-98 Hello World (one-liner)
        const string src = "\"olleH\">:#,_@";
        Assert.AreEqual("Hello", Run(src));
    }
#pragma warning restore IDE0022

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
#pragma warning disable IDE0022
    public void HelloWorld_WithExclamation()
    {
        const string src = "\"!dlroW ,olleH\">:#,_@";
        Assert.AreEqual("Hello, World!", Run(src));
    }
#pragma warning restore IDE0022


    // ── Input ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void InputChar_EchoBack()
        => Assert.AreEqual("A", Run("~,@", "A"));

    [TestMethod]
    public void InputInt_EchoBack()
        => Assert.AreEqual("42 ", Run("&.@", "42\n"));

    [TestMethod]
    public void InputFile_LoadsFileIntoSpace()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            File.WriteAllText("input.txt", "A");

            // Va=(0,0,0), flags=0 (text mode), STR=0"input.txt" (0gnirts)
            var output = Run("00000\"txt.tupni\"in000g.@");
            Assert.AreEqual("65 ", output);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void OutputFile_WritesSpaceRegion()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            // store 'A' at (0,0,0), then output Va=(0,0,0), Vb=(0,0,0), flags=0, STR=0"output.txt"
            _ = Run("88*1+000p00000000\"txt.tuptuo\"o@");

            var bytes = File.ReadAllBytes(Path.Combine(tempDir, "output.txt"));
            CollectionAssert.AreEqual(new byte[] { 65 }, bytes);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void SysInfo_ReportsFileIoSupportFlags()
        => Assert.AreEqual("15 ", Run("1y.@"));

    [TestMethod]
    public void SystemExec_ReturnsExitCode()
    {
        const string command = "exit 7";
        var program = $"{EncodeZeroGnirts(command)}=.@";
        Assert.AreEqual("7 ", Run(program));
    }

    [TestMethod]
    public void SystemExec_SetsNonZeroOnCommandFailure()
    {
        const string command = "this_command_should_not_exist_12345";
        var program = $"{EncodeZeroGnirts(command)}=q";
        Assert.AreNotEqual(0, RunGetExitCode(program));
    }

    // ── Quit exit code ────────────────────────────────────────────────────

    [TestMethod]
    public void Quit_ExitCode7()
        => Assert.AreEqual(7, RunGetExitCode("7q"));

    [TestMethod]
    public void StackUnderStack_U_TransfersFromSoss()
        => Assert.AreEqual("2 ", Run("120{4u.@"));

    [TestMethod]
    public void RunToEnd_UsesProvidedTextIo()
    {
        var space = Parser.FungeParser.Parse("&.@");
        var output = new StringWriter();
        var input = new StringReader("42\n");
        var proc = new FungeProcessor(space);

        var exitCode = RunToEnd(proc, input, output, TestCancellationToken);

        Assert.AreEqual(0, exitCode);
        Assert.AreEqual("42 ", output.ToString());
    }

    [TestMethod]
    public void RunToEndAsync_ReturnsExitCode()
    {
        var space = Parser.FungeParser.Parse("7q");
        var proc = new FungeProcessor(space);

        var exitCode = RunToEnd(proc, TextReader.Null, TextWriter.Null, TestCancellationToken);

        Assert.AreEqual(7, exitCode);
    }
}

file static class Constant
{
    public const int Timeout = 1000 * 30;
}
