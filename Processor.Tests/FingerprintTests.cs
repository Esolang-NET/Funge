using static Esolang.Processor.IOEvent;

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
            [letter] = ctx => ctx.Push(value),
        };
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
[TestClass]
public class FingerprintTests(TestContext TestContext)
{
    CancellationToken TestCancellationToken => TestContext.CancellationToken;

    string Run(string source, IEnumerable<IFingerprint>? fingerprints = null)
    {
        var space = Parser.FungeParser.Parse(source);
        var output = new StringWriter();
        var proc = new FungeProcessor(space, fingerprints: fingerprints);
        var task = Task.Run(async () =>
        {
            await foreach (var ev in proc.RunAsyncEnumerable(TestCancellationToken))
            {
                switch (ev)
                {
                    case OutputCharEvent oce: output.Write(oce.Output); break;
                    case OutputIntEvent oie: output.Write(oie.Output); break;
                }
            }
        }, TestCancellationToken);
        task.GetAwaiter().GetResult();
        return output.ToString();
    }

    // ── Load ( ───────────────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Load_UnknownHandprint_Reflects()
    {
        // No fingerprints registered. '(' reflects → IP goes west → wraps to '@'.
        // "1(@": push 1 (n=1), then '(' pops n=1, pops 1 char (0 from empty stack) →
        //   unknown handprint → reflect → advance west → '1' → wrap to '@' → stop.
        var result = Run("1(@");
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Load_KnownHandprint_PushesSuccess()
    {
        // '(' on success pushes [handprint, 1] (top=1). Verify with output.
        // "TSEP"4( → load "PEST". Stack: [hp,1]. .@ outputs top (1).
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 0);
        var result = Run("\"TSEP\"4(.@", [fp]);
        Assert.AreEqual("1 ", result);
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Load_KnownHandprint_DispatchesInstruction()
    {
        // After loading "PEST" (A→42): stack=[hp,1]; A pushes 42; . outputs "42 ".
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 42);
        var result = Run("\"TSEP\"4(A.@", [fp]);
        Assert.AreEqual("42 ", result);
    }

    // ── Unload ) ─────────────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Unload_KnownHandprint_PushesSuccess()
    {
        // ')' on success pushes [handprint, 1]. Verify with output.
        // Load, discard stack ($$), re-push name, unload. Stack: [hp,1]. .@ outputs 1.
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 0);
        var result = Run("\"TSEP\"4($$\"TSEP\"4).@", [fp]);
        Assert.AreEqual("1 ", result);
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Unload_RevertsSemanticsToReflect()
    {
        // Load "PEST" (A→42): call A → "42 "; discard stack; unload; A now reflects.
        // '#' (trampoline) skips '@'; A reflects west → lands on '@' → stop.
        // Program: "TSEP"4(A.$$"TSEP"4)#@A
        //   '#' skips '@', A reflects → goes west to '@' → stop.
        const string name = "PEST";
        var fp = new PushFingerprint(name, 'A', 42);
        var result = Run("\"TSEP\"4(A.$$\"TSEP\"4)#@A", [fp]);
        Assert.AreEqual("42 ", result);
    }

    // ── A-Z dispatch ─────────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void AZ_WithoutLoad_Reflects()
    {
        // 'A' with no fingerprint loaded → reflect → west → wrap to '@' (row has 2 chars).
        var result = Run("A@");
        Assert.AreEqual(string.Empty, result);
    }

    // ── NULL fingerprint ─────────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void NullFingerprint_AllLettersReflect()
    {
        // NullFingerprint maps every A-Z to Reflect. After loading, 'A' reflects.
        // "NULL" reversed = "LLUN" for correct pop order.
        var fp = NullFingerprint.Instance;
        var result = Run("\"LLUN\"4(A@", [fp]);
        Assert.AreEqual(string.Empty, result);
    }

    // ── Multiple fingerprints ─────────────────────────────────────────────────

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void StackedFingerprints_TopWins()
    {
        // Two fingerprints both define 'A'. Second-loaded takes priority (stack semantics).
        // "FP1A"→A pushes 10; "FP2B"→A pushes 20. Reversed: "A1PF", "B2PF".
        // Load fp1, load fp2, A→20, unload fp2, A→10.
        var fp1 = new PushFingerprint("FP1A", 'A', 10);
        var fp2 = new PushFingerprint("FP2B", 'A', 20);
        var result = Run("\"A1PF\"4(\"B2PF\"4(A.$$\"B2PF\"4)A.@", [fp1, fp2]);
        Assert.AreEqual("20 10 ", result);
    }
}

file static class Constant
{
    public const int Timeout = 1000 * 30;
}
