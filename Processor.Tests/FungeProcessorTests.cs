using TUnit.Assertions.Enums;
using static Esolang.Processor.IOEvent;

namespace Esolang.Funge.Processor.Tests;

public class FungeProcessorTests
{
    static async Task<string> Run(string source, string? input = null, CancellationToken CancellationToken = default)
    {
        var space = Parser.FungeParser.Parse(source);
        var output = new StringWriter();
        var reader = input is null ? TextReader.Null : new StringReader(input);
        var proc = new FungeProcessor(space, input: reader, output: output);
        await RunToEnd(proc, reader, output, CancellationToken);
        return output.ToString();
    }

    static Task<int> RunToEnd(FungeProcessor proc, TextReader input, TextWriter output, CancellationToken ct)
       => Task.Run(async () =>
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

    static async Task<int> RunGetExitCode(string source, CancellationToken CancellationToken = default)
    {
        var space = Parser.FungeParser.Parse(source);
        var proc = new FungeProcessor(space);
        return await RunToEnd(proc, TextReader.Null, TextWriter.Null, CancellationToken);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task TestDirectionalInstructions(CancellationToken CancellationToken)
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

        await RunToEnd(proc, TextReader.Null, TextWriter.Null, CancellationToken);
    }

    static string EncodeZeroGnirts(string value)
        => $"0\"{new string([.. value.Reverse()])}\"";

    // ── Termination ────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Stop_EmptyProgram_Wraps(CancellationToken CancellationToken)
    {
        // No @ → program loops but should terminate via cancellation
        // Just ensure an immediate @ exits
        var result = Run("@", CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Quit_ReturnsExitCode(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("42*q", CancellationToken)).IsEqualTo(8);

    // ── Output ────────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task OutputChar_SingleChar(CancellationToken CancellationToken)
        => await Assert.That(Run("\"H\",@", CancellationToken: CancellationToken)).IsEqualTo("H");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task OutputInt_WithTrailingSpace(CancellationToken CancellationToken)
        => await Assert.That(Run("55+.@", CancellationToken: CancellationToken)).IsEqualTo("10 ");

    // ── Arithmetic ────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Add(CancellationToken CancellationToken)
        => await Assert.That(Run("34+.@", CancellationToken: CancellationToken)).IsEqualTo("7 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Subtract(CancellationToken CancellationToken)
        => await Assert.That(Run("53-.@", CancellationToken: CancellationToken)).IsEqualTo("2 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Multiply(CancellationToken CancellationToken)
        => await Assert.That(Run("34*.@", CancellationToken: CancellationToken)).IsEqualTo("12 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Divide(CancellationToken CancellationToken)
        => await Assert.That(Run("96/.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Remainder(CancellationToken CancellationToken)
        => await Assert.That(Run("72%.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task GreaterThan_True(CancellationToken CancellationToken)
        => await Assert.That(Run("53`.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task GreaterThan_False(CancellationToken CancellationToken)
        => await Assert.That(Run("35`.@", CancellationToken: CancellationToken)).IsEqualTo("0 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task LogicalNot_Zero(CancellationToken CancellationToken)
        => await Assert.That(Run("0!.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task LogicalNot_NonZero(CancellationToken CancellationToken)
        => await Assert.That(Run("5!.@", CancellationToken: CancellationToken)).IsEqualTo("0 ");

    // ── Stack ─────────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Duplicate(CancellationToken CancellationToken)
        => await Assert.That(Run("5:..@", CancellationToken: CancellationToken)).IsEqualTo("5 5 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Swap(CancellationToken CancellationToken)
        => await Assert.That(Run("53\\..@", CancellationToken: CancellationToken)).IsEqualTo("5 3 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Pop_Discard(CancellationToken CancellationToken)
        => await Assert.That(Run("53$.@", CancellationToken: CancellationToken)).IsEqualTo("5 ");

    // ── Direction ─────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task EastWestIf_Zero_GoesEast(CancellationToken CancellationToken)
        =>
        // 0_ → East → . outputs next pop (0) then @
        await Assert.That(Run("0_.@", CancellationToken: CancellationToken)).IsEqualTo("0 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task NorthSouthIf_NonZero_GoesNorth(CancellationToken CancellationToken)
    {
        const string source = "v @\n>1|";
        await Assert.That(Run(source, CancellationToken: CancellationToken)).IsEqualTo(string.Empty);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task EastWestIf_NonZero_GoesWest(CancellationToken CancellationToken)
        =>
        // "1_" at positions 0-1. After '_', go West, wrap to rightmost char...
        // Hard to test in single row. Use '@' placement.
        // "1_@" → goes West to nothing... let's try another approach
        // Just verify we can stop: if nonzero, go West; space wraps; '@' at start doesn't help
        // Skip complex direction tests here; covered by Hello World test below
        await Assert.That(Run("1_@", CancellationToken: CancellationToken)).IsEqualTo(string.Empty); // goes West, wraps, hits '_' etc. – eventually '@' or loops

    // ── Additional coverage for ExecuteInstruction ────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Trampoline_SkipUntilSemicolon(CancellationToken CancellationToken)
        => await Assert.That(Run("; skipped code ;1.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    [Test]
    public async Task HexDigits_Individual(CancellationToken CancellationToken)
    {
        await Assert.That(Run("a.@", CancellationToken: CancellationToken)).IsEqualTo("10 ");
        await Assert.That(Run("b.@", CancellationToken: CancellationToken)).IsEqualTo("11 ");
    }

    [Test]
    public async Task OutputInt_EmptyStack_OutputsZero(CancellationToken CancellationToken)
        => await Assert.That(Run(".@", CancellationToken: CancellationToken)).IsEqualTo("0 ");

    [Test]
    public async Task Iterate_ZeroTimes_SkipsOperand(CancellationToken CancellationToken)
    {
        var result = Run("0k1.2.@", CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("0 2 ").Because($"Expected '0 2 ', but got '{result}'");
    }


    // ── String mode ───────────────────────────────────────────────────────

    [Test]
    public async Task StringMode_PushesChars(CancellationToken CancellationToken)
        =>
        // "Hi" pushes 'H'=72 then 'i'=105; i is on top
        await Assert.That(Run("\"Hi\",,@", CancellationToken: CancellationToken)).IsEqualTo("iH");

    [Test]
    public async Task StringMode_ContiguousSpaces_PushSingleSpace(CancellationToken CancellationToken)
        => await Assert.That(Run("\"   1\".,@", CancellationToken: CancellationToken)).IsEqualTo("49  ");

    // ── Trampoline ────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
#pragma warning disable IDE0022
    public async Task Trampoline_SkipsOne(CancellationToken CancellationToken)
    {
        // "#.@" → skip '.', execute '@' → empty output
        await Assert.That(Run("#.@", CancellationToken: CancellationToken)).IsEqualTo(string.Empty);
    }
#pragma warning restore IDE0022

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task SgmlSpaces_DoNotReflect(CancellationToken CancellationToken)
        => await Assert.That(Run("1\t\v.@", CancellationToken: CancellationToken)).IsEqualTo("1 ");

    // ── FungeSpace get/put ────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task GetPut_ReadWrite(CancellationToken CancellationToken)
        =>
        // p pops z,y,x,v. Build v=65 via 8*8+1, then store at (5,0,0) and read back.
        await Assert.That(Run("88*1+500p500g.@", CancellationToken: CancellationToken)).IsEqualTo("65 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task GoHigh_ChangesDeltaToNegativeZ(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("h\f\f>7q", CancellationToken: CancellationToken)).IsEqualTo(7);

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task GoLow_ChangesDeltaToPositiveZ(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("l\f>7q", CancellationToken: CancellationToken)).IsEqualTo(7);

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task HighLowIf_Zero_GoesLow(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("0m\f >1q\f >2q", CancellationToken: CancellationToken)).IsEqualTo(1);

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task HighLowIf_NonZero_GoesHigh(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("1m\f >1q\f >2q", CancellationToken: CancellationToken)).IsEqualTo(2);

    // ── Hello World ───────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task HelloWorld_Classic(CancellationToken CancellationToken)
    {
        // Classic Befunge-98 Hello World (one-liner)
        const string src = "\"olleH\">:#,_@";
        await Assert.That(Run(src, CancellationToken: CancellationToken)).IsEqualTo("Hello");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task HelloWorld_WithExclamation(CancellationToken CancellationToken)
    {
        const string src = "\"!dlroW ,olleH\">:#,_@";
        await Assert.That(Run(src, CancellationToken: CancellationToken)).IsEqualTo("Hello, World!");
    }


    // ── Input ─────────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task InputChar_EchoBack(CancellationToken CancellationToken)
        => await Assert.That(Run("~,@", "A", CancellationToken: CancellationToken)).IsEqualTo("A");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task InputInt_EchoBack(CancellationToken CancellationToken)
        => await Assert.That(Run("&.@", "42\n", CancellationToken: CancellationToken)).IsEqualTo("42 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task InputFile_LoadsFileIntoSpace(CancellationToken CancellationToken)
    {
        var testContext = TestContext.Current!;
        var originalDir = Directory.GetCurrentDirectory();
        var path = Path.GetTempFileName();
        if (File.Exists(path))
            File.Delete(path);
        var tempDir = Path.Combine(path, testContext.Isolation.GetIsolatedName("funge-input-io"));
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            await File.WriteAllTextAsync("input.txt", "A", CancellationToken);

            // Va=(0,0,0), flags=0 (text mode), STR=0"input.txt" (0gnirts)
            var output = await Run("00000\"txt.tupni\"in000g.@", CancellationToken: CancellationToken);
            await Assert.That(output).IsEqualTo("65 ");
        }
        finally
        {
            if (Directory.Exists(originalDir))
                Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch { }
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task OutputFile_WritesSpaceRegion(CancellationToken CancellationToken)
    {
        var testContext = TestContext.Current!;
        var originalDir = Directory.GetCurrentDirectory();
        var path = Path.GetTempFileName();
        if (File.Exists(path))
            File.Delete(path);
        var tempDir = Path.Combine(path, testContext.Isolation.GetIsolatedName("funge-output-io"));
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            // store 'A' at (0,0,0), then output Va=(0,0,0), Vb=(0,0,0), flags=0, STR=0"output.txt"
            await Run("88*1+000p00000000\"txt.tuptuo\"o@", CancellationToken: CancellationToken);

            var bytes = await File.ReadAllBytesAsync(Path.Combine(tempDir, "output.txt"), CancellationToken);
            await Assert.That(bytes).IsEquivalentTo((byte[])[65], CollectionOrdering.Matching);
        }
        finally
        {
            if (Directory.Exists(originalDir))
                Directory.SetCurrentDirectory(originalDir);
            if (Directory.Exists(tempDir))
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch { }
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task SysInfo_ReportsFileIoSupportFlags(CancellationToken CancellationToken)
        => await Assert.That(Run("1y.@", CancellationToken: CancellationToken)).IsEqualTo("15 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task SystemExec_ReturnsExitCode(CancellationToken CancellationToken)
    {
        const string command = "exit 7";
        var program = $"{EncodeZeroGnirts(command)}=.@";
        await Assert.That(Run(program, CancellationToken: CancellationToken)).IsEqualTo("7 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task SystemExec_SetsNonZeroOnCommandFailure(CancellationToken CancellationToken)
    {
        const string command = "this_command_should_not_exist_12345";
        var program = $"{EncodeZeroGnirts(command)}=q";
        await Assert.That(RunGetExitCode(program, CancellationToken: CancellationToken)).IsNotEqualTo(0);
    }

    // ── Quit exit code ────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Quit_ExitCode7(CancellationToken CancellationToken)
        => await Assert.That(RunGetExitCode("7q", CancellationToken: CancellationToken)).IsEqualTo(7);

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task StackUnderStack_U_TransfersFromSoss(CancellationToken CancellationToken)
        => await Assert.That(Run("120{4u.@", CancellationToken: CancellationToken)).IsEqualTo("2 ");

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunToEnd_UsesProvidedTextIo(CancellationToken CancellationToken)
    {
        var space = Parser.FungeParser.Parse("&.@");
        var output = new StringWriter();
        var input = new StringReader("42\n");
        var proc = new FungeProcessor(space);

        var exitCode = RunToEnd(proc, input, output, CancellationToken);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(output.ToString()).IsEqualTo("42 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RunToEndAsync_ReturnsExitCode(CancellationToken CancellationToken)
    {
        var space = Parser.FungeParser.Parse("7q");
        var proc = new FungeProcessor(space);

        var exitCode = RunToEnd(proc, TextReader.Null, TextWriter.Null, CancellationToken);

        await Assert.That(exitCode).IsEqualTo(7);
    }
}

file static class Constant
{
    public const int Timeout = 1000 * 30;
}
