using Esolang.Funge.Fingerprints.Arry;
using Esolang.Funge.Fingerprints.Base;
using Esolang.Funge.Fingerprints.Fixp;
using Esolang.Funge.Fingerprints.Imth;
using Esolang.Funge.Fingerprints.Indv;
using Esolang.Funge.Fingerprints.Long;
using Esolang.Funge.Fingerprints.Rand;
using TUnit.Assertions.Enums;
using static Esolang.Processor.IOEvent;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor.Tests;

/// <summary>
/// A minimal <see cref="IFingerprint"/> that maps one letter to an instruction that pushes a constant value.
/// </summary>
file sealed class PushFingerprint(string name, char letter, int value) : IFingerprint
{
    public int Handprint { get; } = FingerprintHandprint.Compute(name);
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } =
        new Dictionary<char, FingerprintInstruction>
        {
            [letter] = ctx => { ctx.Push(value); return default; },
        };
}

file sealed class CustomFingerprint(string name, IReadOnlyDictionary<char, FingerprintInstruction> instructions) : IFingerprint
{
    public int Handprint { get; } = FingerprintHandprint.Compute(name);
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } = instructions;
}

file sealed class LifecycleFingerprint(string name, IReadOnlyDictionary<char, FingerprintInstruction> instructions) : IFingerprint, IFungeInstructionPointerLifecycle
{
    public int Handprint { get; } = FingerprintHandprint.Compute(name);
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; } = instructions;
    public List<string> Events { get; } = [];

    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
        => Events.Add($"clone:{parentInstructionPointerId}->{childInstructionPointerId}");

    public void OnInstructionPointerTerminated(int instructionPointerId)
        => Events.Add($"term:{instructionPointerId}");
}

/// <summary>
/// Tests for the fingerprint load <c>(</c>, unload <c>)</c>, and A-Z dispatch semantics.
/// </summary>
/// <remarks>
/// <para>
/// Handprint stack ordering: <c>(</c> pops <c>n</c>, then pops <c>n</c> chars building the handprint as
/// <c>handprint = (handprint &lt;&lt; 8) | pop()</c>. The first popped char contributes the most-significant
/// byte, matching <see cref="FingerprintHandprint.Compute"/>.
/// </para>
/// <para>
/// In string mode <c>"XYZ..."</c> pushes chars left-to-right so the last char is on top.
/// To load fingerprint named <c>"ABCD"</c>, push the reversed string <c>"DCBA"</c> then count <c>4</c>.
/// For example: <c>"PEST"</c> → push <c>"TSEP"</c>; <c>"NULL"</c> → push <c>"LLUN"</c>.
/// </para>
/// </remarks>
public class FingerprintTests
{
    static async Task<string> Run(string source, IEnumerable<IFingerprint>? fingerprints = null, string? input = null, CancellationToken CancellationToken = default)
        => await Run(Parser.FungeParser.Parse(source), fingerprints, input, CancellationToken);

    static async Task<string> Run(Parser.FungeSpace space, IEnumerable<IFingerprint>? fingerprints = null, string? input = null, CancellationToken CancellationToken = default)
    {
        var testContext = TestContext.Current!;
        var output = new StringWriter();
        var reader = input is null ? TextReader.Null : new StringReader(input);
        var proc = new FungeProcessor(space, fingerprints: fingerprints);
        await Task.Run(async () =>
        {
            await foreach (var ev in proc.RunAsyncEnumerable(CancellationToken))
            {
                testContext.OutputWriter.WriteLine($"Event: {ev.GetType().Name}");
                switch (ev)
                {
                    case OutputCharEvent oce: output.Write(oce.Output); break;
                    case OutputIntEvent oie: output.Write(oie.Output); break;
                    case OutputStringEvent ose: output.Write(ose.Output); break;
                    case OutputLineEvent ole: output.WriteLine(ole.Output); break;
                    case InputCharEvent ice:
                        var c = reader.Read();
                        if (c != -1) ice.Write((char)c);
                        break;
                    case InputIntEvent iie:
                        var line = reader.ReadLine();
                        if (int.TryParse(line, out var val)) iie.Write(val);
                        break;
                    case InputStringEvent ise:
                        var str = reader.ReadLine();
                        if (str is not null) ise.Write(str);
                        break;
                    case InputLineEvent ile:
                        var line2 = reader.ReadLine();
                        if (line2 is not null) ile.Write(line2);
                        break;
                    case EndEvent:
                        break;
                    default: throw new InvalidOperationException($"Unexpected event: {ev.GetType().Name}");
                }
            }
        }, CancellationToken);
        return output.ToString();
    }

    static async Task<int> RunExitCode(string source, IEnumerable<IFingerprint>? fingerprints = null, string? input = null, bool provideInput = true, bool provideOutput = true, CancellationToken CancellationToken = default)
    {
        var space = Parser.FungeParser.Parse(source);
        var output = new StringWriter();
        var reader = input is null ? TextReader.Null : new StringReader(input);
        var proc = new FungeProcessor(
            space,
            fingerprints: fingerprints,
            enableInput: provideInput,
            enableOutput: provideOutput
        );
        return await Task.Run(async () =>
        {
            var exitCode = 0;
            await foreach (var ev in proc.RunAsyncEnumerable(CancellationToken))
            {
                switch (ev)
                {
                    case OutputCharEvent oce: output.Write(oce.Output); break;
                    case OutputIntEvent oie: output.Write(oie.Output); break;
                    case OutputStringEvent ose: output.Write(ose.Output); break;
                    case OutputLineEvent ole: output.WriteLine(ole.Output); break;
                    case InputCharEvent ice:
                        var c = reader.Read();
                        if (c != -1) ice.Write((char)c);
                        break;
                    case InputIntEvent iie:
                        var line = reader.ReadLine();
                        if (int.TryParse(line, out var val)) iie.Write(val);
                        break;
                    case InputStringEvent ise:
                        var str = reader.ReadLine();
                        if (str is not null) ise.Write(str);
                        break;
                    case InputLineEvent ile:
                        var line2 = reader.ReadLine();
                        if (line2 is not null) ile.Write(line2);
                        break;
                    case EndEvent ee:
                        exitCode = ee.ExitCode;
                        break;
                    default: throw new InvalidOperationException($"Unexpected event: {ev.GetType().Name}");
                }
            }

            return exitCode;
        }, CancellationToken);
    }

    // ── Load ( ───────────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Load_UnknownHandprint_Reflects(CancellationToken CancellationToken)
    {
        // No fingerprints registered. '(' reflects → IP goes west → wraps to '@'.
        // "1(@": push 1 (n=1), then '(' pops n=1, pops 1 char (0 from empty stack) →
        //   unknown handprint → reflect → advance west → '1' → wrap to '@' → stop.
        var result = Run("1(@", CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Load_KnownHandprint_PushesSuccess(CancellationToken CancellationToken)
    {
        // '(' on success pushes [handprint, 1] (top=1). Verify with output.
        // "TSEP"4( → load "PEST". Stack: [hp,1]. .@ outputs top (1).
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 0);
        var result = Run("\"TSEP\"4(.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("1 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Load_KnownHandprint_DispatchesInstruction(CancellationToken CancellationToken)
    {
        // After loading "PEST" (A→42): stack=[hp,1]; A pushes 42; . outputs "42 ".
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 42);
        var result = Run("\"TSEP\"4(A.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("42 ");
    }

    // ── Unload ) ─────────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Unload_KnownHandprint_PushesSuccess(CancellationToken CancellationToken)
    {
        // ')' on success pushes [handprint, 1]. Verify with output.
        // Load, discard stack ($$), re-push name, unload. Stack: [hp,1]. .@ outputs 1.
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 0);
        var result = Run("\"TSEP\"4($$\"TSEP\"4).@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("1 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Unload_RevertsSemanticsToReflect(CancellationToken CancellationToken)
    {
        // Load "PEST" (A→42): call A → "42 "; discard stack; unload; A now reflects.
        // '#' (trampoline) skips '@'; A reflects west → lands on '@' → stop.
        // Program: "TSEP"4(A.$$"TSEP"4)#@A
        //   '#' skips '@', A reflects → goes west to '@' → stop.
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 42);
        var result = Run("\"TSEP\"4(A.$$\"TSEP\"4)#@A", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("42 ");
    }

    // ── A-Z dispatch ─────────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task AZ_WithoutLoad_Reflects(CancellationToken CancellationToken)
    {
        // 'A' with no fingerprint loaded → reflect → west → wrap to '@' (row has 2 chars).
        var result = Run("A@", CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    // ── NULL fingerprint ─────────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task NullFingerprint_AllLettersReflect(CancellationToken CancellationToken)
    {
        // NullFingerprint maps every A-Z to Reflect. After loading, 'A' reflects.
        // "NULL" reversed = "LLUN" for correct pop order.
        var fp = NullFingerprint.Instance;
        var result = Run("\"LLUN\"4(#@A", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task NullFingerprint_AllKeys()
    {
        char[] expected = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'];
        var actual = NullFingerprint.Keys.ToArray();
        await Assert.That(actual).IsEquivalentTo(expected, CollectionOrdering.Matching);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task BaseFingerprint_OutputUsesExecutionContextIo(CancellationToken CancellationToken)
    {
        var result = Run("\"ESAB\"4(5B@", [new BaseFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("101");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task BaseFingerprint_InputUsesExecutionContextIo(CancellationToken CancellationToken)
    {
        var result = Run("\"ESAB\"4(2I.@", [new BaseFingerprint()], "101", CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("5 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task BaseFingerprint_OutputReflectsWithoutOutputCapability(CancellationToken CancellationToken)
    {
        var result = RunExitCode("\"ESAB\"4(5#@B1q", [new BaseFingerprint()], provideOutput: false, CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task BaseFingerprint_InputReflectsWithoutInputCapability(CancellationToken CancellationToken)
    {
        var result = RunExitCode("\"ESAB\"4(2#@I1q", [new BaseFingerprint()], provideInput: false, CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task ImthFingerprint_UnsignedOutputUsesExecutionContextIo(CancellationToken CancellationToken)
    {
        var result = Run("\"HTMI\"4(5U@", [new IntegerMathFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("5 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task ImthFingerprint_UnsignedOutputReflectsWithoutOutputCapability(CancellationToken CancellationToken)
    {
        var result = RunExitCode("\"HTMI\"4(5#@U1q", [new IntegerMathFingerprint()], provideOutput: false, CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task LongFingerprint_OutputUsesExecutionContextIo(CancellationToken CancellationToken)
    {
        var result = Run("\"GNOL\"4(0n5EP@", [new LongIntegerFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("5 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task LongFingerprint_OutputReflectsWithoutOutputCapability(CancellationToken CancellationToken)
    {
        var result = await RunExitCode("\"GNOL\"4(0n5E#@P1q", [new LongIntegerFingerprint()], provideOutput: false, CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo(0);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FixpFingerprint_ComputesIntegerSquareRoot(CancellationToken CancellationToken)
    {
        var result = Run("\"PXIF\"4(98*2*Q.@", [new FixedPointFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("12 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RandFingerprint_PushesMaximumInteger(CancellationToken CancellationToken)
    {
        var result = Run("\"DNAR\"4(M.@", [new RandomFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo($"{int.MaxValue} ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task IndvFingerprint_ReadsCellThroughIndirectAddress(CancellationToken CancellationToken)
    {
        var space = Parser.FungeParser.Parse("\"VDNI\"4($$45*00G.@");
        space[new Parser.FungeVector(20, 0, 0)] = 0;
        space[new Parser.FungeVector(21, 0, 0)] = 0;
        space[new Parser.FungeVector(22, 0, 0)] = 30;
        space[new Parser.FungeVector(30, 0, 0)] = 42;

        var result = Run(space, [new IndirectVectorFingerprint()], CancellationToken: CancellationToken);

        await Assert.That(result).IsEqualTo("42 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task ArryFingerprint_G_ReportsMaximumDimensions(CancellationToken CancellationToken)
    {
        var result = Run("\"YRRA\"4(G.@", [new ArrayFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("3 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task ArryFingerprint_StoresAndRetrievesFromAbsoluteSpace(CancellationToken CancellationToken)
    {
        var result = Run("\"YRRA\"4($$01067*2A2B.@", [new ArrayFingerprint()], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("42 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeContext_ExposesStorageOffsetCapability(CancellationToken CancellationToken)
    {
        var fp = new CustomFingerprint(
            "PEST",
            new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
            {
                ['A'] = ctx =>
                {
                    if (ctx is not IFungeStorageOffsetContext offset)
                    {
                        ctx.Reflect();
                        return default;
                    }

                    ctx.Push(offset.StorageOffset.X);
                    return default;
                },
            });
        var result = Run("\"TSEP\"4(A.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("0 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeContext_ExposesRandomCapability(CancellationToken CancellationToken)
    {
        var fp = new CustomFingerprint(
            "PEST",
            new Dictionary<char, FingerprintInstruction>
            {
                ['A'] = ctx =>
                {
                    if (ctx is not IFungeRandomContext random)
                    {
                        ctx.Reflect();
                        return default;
                    }

                    random.Reseed(123u);
                    ctx.Push(random.NextUInt32(1) == 0 ? 1 : 0);
                    return default;
                },
            });
        var result = Run("\"TSEP\"4(A.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("1 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeContext_ExposesInstructionPointerCapability(CancellationToken CancellationToken)
    {
        var fp = new CustomFingerprint(
            "PEST",
            new Dictionary<char, FingerprintInstruction>
            {
                ['A'] = ctx =>
                {
                    if (ctx is not IFungeInstructionPointerContext instructionPointer)
                    {
                        ctx.Reflect();
                        return default;
                    }

                    ctx.Push(instructionPointer.InstructionPointerId);
                    return default;
                },
            });
        var result = Run("\"TSEP\"4(A.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("0 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeContext_ExposesStackCapability(CancellationToken CancellationToken)
    {
        var fp = new CustomFingerprint(
            "PEST",
            new Dictionary<char, FingerprintInstruction>
            {
                ['A'] = ctx =>
                {
                    if (ctx is not IFungeStackContext stack)
                    {
                        ctx.Reflect();
                        return default;
                    }

                    ctx.Push(stack.StackDepth);
                    return default;
                },
            });
        var result = Run("\"TSEP\"4($$12A.@", [fp], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("2 ");
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeLifecycle_NotifiesCloneAndTermination(CancellationToken CancellationToken)
    {
        var fp = new LifecycleFingerprint("PEST", new Dictionary<char, FingerprintInstruction>());
        var exitCode = RunExitCode("\"TSEP\"4(>tq", [fp], CancellationToken: CancellationToken);
        await Assert.That(exitCode).IsEqualTo(1);
        await Assert.That(fp.Events).IsEquivalentTo((string[])["clone:0->1", "term:0", "term:1"], CollectionOrdering.Matching);
    }

    // ── Multiple fingerprints ─────────────────────────────────────────────────

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task StackedFingerprints_TopWins(CancellationToken CancellationToken)
    {
        // Two fingerprints both define 'A'. Second-loaded takes priority (stack semantics).
        // "FP1A"→A pushes 10; "FP2B"→A pushes 20. Reversed: "A1PF", "B2PF".
        // Load fp1, load fp2, A→20, unload fp2, A→10.
        var fp1 = new PushFingerprint("FP1A", 'A', 10);
        var fp2 = new PushFingerprint("FP2B", 'A', 20);
        var result = Run("\"A1PF\"4(\"B2PF\"4(A.$$\"B2PF\"4)A.@", [fp1, fp2], CancellationToken: CancellationToken);
        await Assert.That(result).IsEqualTo("20 10 ");
    }
}

file static class Constant
{
    public const int Timeout = 1000 * 30;
}
