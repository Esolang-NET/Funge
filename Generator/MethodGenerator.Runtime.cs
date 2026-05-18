using System.Text;

namespace Esolang.Funge.Generator;

partial class MethodGenerator
{
    const string FungeRuntimeFileName = "FungeRuntime.g.cs";

    [System.Flags]
    enum RuntimeFacadeFeatures
    {
        None = 0, RunSync = 1 << 0, RunString = 1 << 1, RunEnumerable = 1 << 2, RunAsyncEnumerable = 1 << 3,
        RunTask = 1 << 4, RunTaskInt = 1 << 5, RunTaskString = 1 << 6, RunValueTask = 1 << 7,
        RunValueTaskInt = 1 << 8, RunValueTaskString = 1 << 9,
    }

    static void EmitRuntimeIfNeeded(Microsoft.CodeAnalysis.SourceProductionContext ctx, RuntimeFacadeFeatures features)
    {
        if (features == RuntimeFacadeFeatures.None) return;
        ctx.AddSource(FungeRuntimeFileName, BuildRuntimeSource(features));
    }

    static string BuildRuntimeFacadeMethods(RuntimeFacadeFeatures features)
    {
        var sb = new StringBuilder();
        if ((features & RuntimeFacadeFeatures.RunSync) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static int RunSync(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default)
                    => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct);
        """);
        if ((features & RuntimeFacadeFeatures.RunString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static string RunString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default)
                {
                    using var output = new StringWriter();
                    RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct);
                    return output.ToString();
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunEnumerable) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static IEnumerable<byte> RunEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default)
                {
                    foreach (var b in RunCoreEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, ct))
                        yield return b;
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunAsyncEnumerable) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static async IAsyncEnumerable<byte> RunAsyncEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, [EnumeratorCancellation] CancellationToken ct = default)
                {
                    await foreach (var b in RunCoreAsyncEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, ct))
                        yield return b;
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunTask) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task RunTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct), ct);
        """);
        if ((features & RuntimeFacadeFeatures.RunTaskInt) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<int> RunTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct), ct);
        """);
        if ((features & RuntimeFacadeFeatures.RunTaskString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<string> RunTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default)
                {
                    return Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct);
                        return output.ToString();
                    }, ct);
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTask) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask RunValueTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct), ct));
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTaskInt) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<int> RunValueTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct), ct));
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTaskString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<string> RunValueTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default)
                {
                    return new ValueTask<string>(Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct);
                        return output.ToString();
                    }, ct));
                }
        """);
        return sb.ToString();
    }

    static string BuildRuntimeSource(RuntimeFacadeFeatures features) => string.Concat("""
        // <auto-generated/>
        #nullable enable
        #pragma warning disable CS1591
        using System;
        using System.Collections;
        using System.Collections.Generic;
        using System.Diagnostics;
        using System.IO;
        using System.Linq;
        using System.Runtime.CompilerServices;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Esolang.Funge.__Generated
        {
            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            internal static class FungeRuntime
            {
        """,
        BuildRuntimeFacadeMethods(features), """
                private sealed class RuntimeStackStack
                {
                    private readonly LinkedList<Stack<int>> _stacks = new LinkedList<Stack<int>>();
                    internal RuntimeStackStack() { _stacks.AddFirst(new Stack<int>()); }
                    internal RuntimeStackStack(LinkedList<Stack<int>> stacks) { foreach (var stack in stacks) _stacks.AddLast(new Stack<int>(stack.Reverse())); }
                    internal Stack<int> TOSS { get { return _stacks.First!.Value; } }
                    internal Stack<int> SOSS { get { return _stacks.First!.Next!.Value; } }
                    internal bool HasSOSS { get { return _stacks.Count >= 2; } }
                    internal int StackCount { get { return _stacks.Count; } }
                    internal IEnumerable<Stack<int>> AllStacks { get { return _stacks; } }
                    internal void Push(int value) { TOSS.Push(value); }
                    internal int Pop() { return TOSS.Count > 0 ? TOSS.Pop() : 0; }
                    internal void ClearToss() { TOSS.Clear(); }
                    internal void PushNewStack() { _stacks.AddFirst(new Stack<int>()); }
                    internal void PopCurrentStack() { if (_stacks.Count > 1) _stacks.RemoveFirst(); }
                    internal RuntimeStackStack Clone() => new RuntimeStackStack(_stacks);
                }

                private sealed class RuntimeIp
                {
                    internal RuntimeIp(int id) { Id = id; Delta = (1, 0, 0); StackStack = new RuntimeStackStack(); }
                    internal int Id { get; }
                    internal (int X, int Y, int Z) Position;
                    internal (int X, int Y, int Z) Delta;
                    internal (int X, int Y, int Z) Offset;
                    internal RuntimeStackStack StackStack;
                    internal bool StringMode;
                    internal bool IsStopped;
                    internal RuntimeIp CreateChild(int newId)
                    {
                        return new RuntimeIp(newId) { Position = Position, Delta = (-Delta.X, -Delta.Y, -Delta.Z), Offset = Offset, StackStack = StackStack.Clone(), StringMode = StringMode };
                    }
                }

                private static int RunCore(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct)
                {
                    return Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (byte b) => output.Write((char)b), ct);
                }

                private static IEnumerable<byte> RunCoreEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct)
                {
                    var output = new System.Collections.Generic.List<byte>();
                    Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (byte b) => output.Add(b), ct);
                    return output;
                }

                private static async IAsyncEnumerable<byte> RunCoreAsyncEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, [EnumeratorCancellation] CancellationToken ct)
                {
                    var buffer = new System.Collections.Concurrent.ConcurrentQueue<byte>();
                    var tcs = new TaskCompletionSource<int>();
                    await Task.Run(() => {
                        try {
                            Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (byte b) => buffer.Enqueue(b), ct);
                            tcs.SetResult(0);
                        } catch (Exception ex) { tcs.SetException(ex); }
                    }, ct);
                    while (!tcs.Task.IsCompleted || !buffer.IsEmpty)
                    {
                        while (buffer.TryDequeue(out var b)) yield return b;
                        if (tcs.Task.IsCompleted) break;
                        await Task.Delay(10, ct);
                    }
                    await tcs.Task;
                }

                private static int Execute(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, Action<byte> writeOutput, CancellationToken ct)
                {
                    var rng = new Random();
                    int exitCode = 0;
                    bool quit = false;
                    int GetCell(int x, int y, int z) => cells.TryGetValue((x, y, z), out var value) ? value : ' ';
                    void SetCell(int x, int y, int z, int value) { if (value == ' ') cells.Remove((x, y, z)); else cells[(x, y, z)] = value; }
                    (int X, int Y, int Z) Advance((int X, int Y, int Z) pos, (int X, int Y, int Z) delta)
                    {
                        int nx = pos.X + delta.X, ny = pos.Y + delta.Y, nz = pos.Z + delta.Z;
                        int width = maxX - minX + 1, height = maxY - minY + 1, depth = maxZ - minZ + 1;
                        if (nx < minX) nx = maxX - ((minX - nx - 1) % width); else if (nx > maxX) nx = minX + ((nx - maxX - 1) % width);
                        if (ny < minY) ny = maxY - ((minY - ny - 1) % height); else if (ny > maxY) ny = minY + ((ny - maxY - 1) % height);
                        if (nz < minZ) nz = maxZ - ((minZ - nz - 1) % depth); else if (nz > maxZ) nz = minZ + ((nz - maxZ - 1) % depth);
                        return (nx, ny, nz);
                    }
                    (int X, int Y, int Z) PopVector(RuntimeStackStack stack) => (stack.Pop(), stack.Pop(), stack.Pop());
                    
                    void ExecuteInstruction(RuntimeIp ip, LinkedListNode<RuntimeIp> ipNode, ref bool suppressAdvance, int? overrideCell)
                    {
                        int cell = overrideCell ?? GetCell(ip.Position.X, ip.Position.Y, ip.Position.Z);
                        switch (cell)
                        {
                            case 'p': { int z = ip.StackStack.Pop(); int y = ip.StackStack.Pop(); int x = ip.StackStack.Pop(); int v = ip.StackStack.Pop(); SetCell(x + ip.Offset.X, y + ip.Offset.Y, z + ip.Offset.Z, v); } break;
                            case 'x': { var v = PopVector(ip.StackStack); ip.Delta = v; } break;
                            case '.': if (!hasOutput) throw new InvalidOperationException("Output '.'"); writeOutput((byte)ip.StackStack.Pop()); writeOutput((byte)' '); break;
                            case ',': if (!hasOutput) throw new InvalidOperationException("Output ','"); writeOutput((byte)ip.StackStack.Pop()); break;
                            case '@': ip.IsStopped = true; break;
                            case 'q': exitCode = ip.StackStack.Pop(); quit = true; break;
                            default: if (cell >= 'A' && cell <= 'Z') ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break;
                        }
                    }

                    var ips = new LinkedList<RuntimeIp>();
                    ips.AddFirst(new RuntimeIp(0));
                    while (ips.Count > 0 && !quit)
                    {
                        ct.ThrowIfCancellationRequested();
                        var node = ips.First;
                        while (node != null && !quit)
                        {
                            var nextNode = node.Next;
                            var ip = node.Value;
                            bool suppressAdvance = false;
                            ExecuteInstruction(ip, node, ref suppressAdvance, null);
                            if (ip.IsStopped || quit) ips.Remove(node);
                            else if (!suppressAdvance) ip.Position = Advance(ip.Position, ip.Delta);
                            node = nextNode;
                        }
                    }
                    return exitCode;
                }
            }
        }
        """);
}
