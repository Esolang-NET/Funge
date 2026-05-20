using System.Text;

namespace Esolang.Funge.Generator;

partial class MethodGenerator
{
    const string FungeRuntimeFileName = "FungeRuntime.g.cs";

    [System.Flags]
    enum RuntimeFacadeFeatures
    {
        None = 0, 
        RunSync = 1 << 0, 
        RunString = 1 << 1, 
        RunEnumerable = 1 << 2, 
        RunAsyncEnumerable = 1 << 3,
        RunTask = 1 << 4, 
        RunTaskInt = 1 << 5, 
        RunTaskString = 1 << 6, 
        RunValueTask = 1 << 7,
        RunValueTaskInt = 1 << 8, 
        RunValueTaskString = 1 << 9,
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
                internal static int RunSync(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                    => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs);
        """);
        if ((features & RuntimeFacadeFeatures.RunString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static string RunString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                {
                    using var output = new StringWriter();
                    RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs);
                    return output.ToString();
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunEnumerable) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static IEnumerable<byte> RunEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                {
                    foreach (var b in RunCoreEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, ct, args, envs))
                        yield return b;
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunAsyncEnumerable) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static async IAsyncEnumerable<byte> RunAsyncEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, [EnumeratorCancellation] CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                {
                    await foreach (var b in RunCoreAsyncEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, ct, args, envs))
                        yield return b;
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunTask) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task RunTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs), ct);
        """);
        if ((features & RuntimeFacadeFeatures.RunTaskInt) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<int> RunTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs), ct);
        """);
        if ((features & RuntimeFacadeFeatures.RunTaskString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<string> RunTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                {
                    return Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs);
                        return output.ToString();
                    }, ct);
                }
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTask) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask RunValueTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs), ct));
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTaskInt) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<int> RunValueTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs), ct));
        """);
        if ((features & RuntimeFacadeFeatures.RunValueTaskString) != 0)
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<string> RunValueTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct = default, IEnumerable<string>? args = null, IEnumerable<string>? envs = null)
                {
                    return new ValueTask<string>(Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, ct, args, envs);
                        return output.ToString();
                    }, ct));
                }
        """);
        return sb.ToString();
    }

    static string BuildRuntimeSource(RuntimeFacadeFeatures features) => $$"""
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
        using System.Runtime.InteropServices;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Esolang.Funge.__Generated
        {
            [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
            internal static class FungeRuntime
            {
        {{BuildRuntimeFacadeMethods(features)}}
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

                private static int RunCore(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken ct, IEnumerable<string>? args, IEnumerable<string>? envs)
                {
                    return Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (v) => output.Write(v.ToString()), (c) => output.Write((char)c), ct, args, envs);
                }

                private static IEnumerable<byte> RunCoreEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken ct, IEnumerable<string>? args, IEnumerable<string>? envs)
                {
                    var output = new System.Collections.Generic.List<byte>();
                    Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (v) => { foreach(var b in System.Text.Encoding.ASCII.GetBytes(v.ToString())) output.Add(b); }, (c) => output.Add((byte)c), ct, args, envs);
                    return output;
                }

                private static async IAsyncEnumerable<byte> RunCoreAsyncEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, [EnumeratorCancellation] CancellationToken ct, IEnumerable<string>? args, IEnumerable<string>? envs)
                {
                    var buffer = new System.Collections.Concurrent.ConcurrentQueue<byte>();
                    var tcs = new TaskCompletionSource<int>();
                    await Task.Run(() => {
                        try {
                            Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (v) => { foreach(var b in System.Text.Encoding.ASCII.GetBytes(v.ToString())) buffer.Enqueue(b); }, (c) => buffer.Enqueue((byte)c), ct, args, envs);
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

                private static int Execute(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, Action<int> writeOutputInt, Action<int> writeOutputChar, CancellationToken ct, IEnumerable<string>? args, IEnumerable<string>? envs)
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
                    (int X, int Y, int Z) PopVector(RuntimeStackStack stack)
                    {
                        int z = stack.Pop(), y = stack.Pop(), x = stack.Pop();
                        return (x, y, z);
                    }
                    void PushVector(RuntimeStackStack stack, (int X, int Y, int Z) v)
                    {
                        stack.Push(v.X); stack.Push(v.Y); stack.Push(v.Z);
                    }

                    bool TryPopZeroTerminatedString(RuntimeStackStack stack, out string result)
                    {
                        var chars = new List<char>();
                        while (true)
                        {
                            var value = stack.Pop();
                            if (value == 0) { result = new string(chars.ToArray()); return true; }
                            if (value < char.MinValue || value > char.MaxValue) { result = string.Empty; return false; }
                            chars.Add((char)value);
                        }
                    }

                    void PushSysInfo(RuntimeIp ip, int ipCount, int c)
                    {
                        var items = new List<int>();
                        items.Add(0x01 | 0x02 | 0x04 | 0x08); // Flags: t, i, o, = supported
                        items.Add(4); // Cell size
                        items.Add(unchecked((int)0x46756E67u)); // handprint "Fung"
                        items.Add(9800); // version
                        items.Add(1); // paradigm
                        items.Add(Path.DirectorySeparatorChar);
                        items.Add(3); // dims
                        items.Add(ip.Id);
                        items.Add(0); // team
                        items.Add(ip.Position.X); items.Add(ip.Position.Y); items.Add(ip.Position.Z);
                        items.Add(ip.Delta.X); items.Add(ip.Delta.Y); items.Add(ip.Delta.Z);
                        items.Add(ip.Offset.X); items.Add(ip.Offset.Y); items.Add(ip.Offset.Z);
                        items.Add(minX); items.Add(minY); items.Add(minZ);
                        items.Add(maxX - minX); items.Add(maxY - minY); items.Add(maxZ - minZ);
                        var now = DateTime.Now;
                        items.Add(((now.Year - 1900) * 256 * 256) + (now.Month * 256) + now.Day);
                        items.Add((now.Hour * 256 * 256) + (now.Minute * 256) + now.Second);
                        items.Add(ip.StackStack.StackCount);
                        foreach (var stack in ip.StackStack.AllStacks) items.Add(stack.Count);

                        var resolvedArgs = args ?? Environment.GetCommandLineArgs();
                        foreach (var arg in resolvedArgs)
                        {
                            foreach (var ch in arg) items.Add(ch);
                            items.Add(0);
                        }
                        items.Add(0);

                        var resolvedEnvs = envs ?? Environment.GetEnvironmentVariables()
                            .Cast<DictionaryEntry>()
                            .Select(entry => $"{entry.Key}={entry.Value}");
                        foreach (var env in resolvedEnvs)
                        {
                            foreach (var ch in env) items.Add(ch);
                            items.Add(0);
                        }
                        items.Add(0);

                        for (int i = items.Count - 1; i >= 0; i--) ip.StackStack.Push(items[i]);
                        if (c > 0)
                        {
                            var snapshot = ip.StackStack.TOSS.ToArray();
                            int picked = c <= snapshot.Length ? snapshot[c - 1] : 0;
                            for (int i = 0; i < items.Count; i++) ip.StackStack.Pop();
                            ip.StackStack.Push(picked);
                        }
                    }

                    bool TryInputFile((int X, int Y, int Z) leastPoint, string fileName, bool binaryMode, out (int X, int Y, int Z) size)
                    {
                        size = (0, 0, 0);
                        byte[] bytes;
                        try { bytes = File.ReadAllBytes(fileName); } catch { return false; }
                        int x = 0, y = 0, z = 0, maxX = 0, maxY = 0, maxZ = 0;
                        bool wroteAny = false;
                        foreach (var raw in bytes)
                        {
                            int cell = (int)raw;
                            if (!binaryMode)
                            {
                                if (cell == '\r') continue;
                                if (cell == '\n') { x = 0; y++; continue; }
                                if (cell == '\f') { x = 0; y = 0; z++; continue; }
                                if (cell == '\t' || cell == '\v') cell = ' ';
                            }
                            SetCell(leastPoint.X + x, leastPoint.Y + y, leastPoint.Z + z, cell);
                            wroteAny = true;
                            if (x > maxX) maxX = x; if (y > maxY) maxY = y; if (z > maxZ) maxZ = z;
                            x++;
                        }
                        size = wroteAny ? (maxX, maxY, maxZ) : (0, 0, 0);
                        return true;
                    }

                    bool TryOutputFile((int X, int Y, int Z) leastPoint, (int X, int Y, int Z) size, string fileName, bool linearText)
                    {
                        int sx = Math.Max(0, size.X), sy = Math.Max(0, size.Y), sz = Math.Max(0, size.Z);
                        var rows = new List<string>();
                        for (int z = 0; z <= sz; z++)
                        {
                            for (int y = 0; y <= sy; y++)
                            {
                                var chars = new char[sx + 1];
                                for (int x = 0; x <= sx; x++)
                                {
                                    int c = GetCell(leastPoint.X + x, leastPoint.Y + y, leastPoint.Z + z);
                                    chars[x] = (c >= char.MinValue && c <= char.MaxValue) ? (char)c : ' ';
                                }
                                var row = new string(chars);
                                rows.Add(linearText ? row.TrimEnd(' ') : row);
                            }
                            if (z != sz) rows.Add("\f");
                        }
                        if (linearText) while (rows.Count > 0 && rows[rows.Count - 1].Length == 0) rows.RemoveAt(rows.Count - 1);
                        var text = string.Join("\n", rows);
                        var bytes = text.Select(ch => (byte)(ch & 0xFF)).ToArray();
                        try { File.WriteAllBytes(fileName, bytes); return true; } catch { return false; }
                    }

                    int ExecuteSystemCommand(string command)
                    {
                        try
                        {
                            var psi = new ProcessStartInfo { UseShellExecute = false, CreateNoWindow = true };
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                psi.FileName = "cmd.exe";
        #if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                                psi.ArgumentList.Add("/c");
                                psi.ArgumentList.Add(command);
        #else
                                psi.Arguments = "/c \"" + command.Replace("\"", "\"\"") + "\"";
        #endif
                            }
                            else
                            {
                                psi.FileName = "/bin/sh";
        #if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
                                psi.ArgumentList.Add("-c");
                                psi.ArgumentList.Add(command);
        #else
                                psi.Arguments = "-c \"" + command.Replace("\"", "\\\"") + "\"";
        #endif
                            }
                            using var process = Process.Start(psi);
                            if (process == null) return -1;
                            process.WaitForExit();
                            return process.ExitCode;
                        } catch { return -1; }
                    }

                    var ips = new LinkedList<RuntimeIp>();
                    void ExecuteInstruction(RuntimeIp ip, LinkedListNode<RuntimeIp> ipNode, ref bool suppressAdvance, int? overrideCell)
                    {
                        int cell = overrideCell ?? GetCell(ip.Position.X, ip.Position.Y, ip.Position.Z);
                        if (ip.StringMode)
                        {
                            if (cell == '"') ip.StringMode = false;
                            else if (cell == ' ' || cell == '\t' || cell == '\f' || cell == '\v')
                            {
                                ip.StackStack.Push(' ');
                                while (true)
                                {
                                    var next = Advance(ip.Position, ip.Delta);
                                    var nextCell = GetCell(next.X, next.Y, next.Z);
                                    if (nextCell == ' ' || nextCell == '\t' || nextCell == '\f' || nextCell == '\v') ip.Position = next;
                                    else break;
                                }
                            }
                            else ip.StackStack.Push(cell);
                            return;
                        }

                        switch (cell)
                        {
                            case '"': ip.StringMode = true; break;
                            case ' ': case '\t': case '\f': case '\v': case 'z': break;
                            case '!': ip.StackStack.Push(ip.StackStack.Pop() == 0 ? 1 : 0); break;
                            case '$': ip.StackStack.Pop(); break;
                            case ':': { int v = ip.StackStack.Pop(); ip.StackStack.Push(v); ip.StackStack.Push(v); break; }
                            case '\\': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(b); ip.StackStack.Push(a); break; }
                            case 'n': ip.StackStack.ClearToss(); break;
                            case '+': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(a + b); break; }
                            case '-': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(a - b); break; }
                            case '*': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(a * b); break; }
                            case '/': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(b == 0 ? 0 : a / b); break; }
                            case '%': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(b == 0 ? 0 : a % b); break; }
                            case '`': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); ip.StackStack.Push(a > b ? 1 : 0); break; }
                            case '0': case '1': case '2': case '3': case '4': case '5': case '6': case '7': case '8': case '9': ip.StackStack.Push(cell - '0'); break;
                            case 'a': case 'b': case 'c': case 'd': case 'e': case 'f': ip.StackStack.Push(cell - 'a' + 10); break;
                            case '>': ip.Delta = (1, 0, 0); break;
                            case '<': ip.Delta = (-1, 0, 0); break;
                            case '^': ip.Delta = (0, -1, 0); break;
                            case 'v': ip.Delta = (0, 1, 0); break;
                            case 'h': ip.Delta = (0, 0, -1); break;
                            case 'l': ip.Delta = (0, 0, 1); break;
                            case '?': switch (rng.Next(6)) { case 0: ip.Delta = (1, 0, 0); break; case 1: ip.Delta = (-1, 0, 0); break; case 2: ip.Delta = (0, -1, 0); break; case 3: ip.Delta = (0, 1, 0); break; case 4: ip.Delta = (0, 0, -1); break; default: ip.Delta = (0, 0, 1); break; } break;
                            case 'm': { int v = ip.StackStack.Pop(); ip.Delta = v == 0 ? (0, 0, 1) : (0, 0, -1); break; }
                            case '_': { int v = ip.StackStack.Pop(); ip.Delta = v == 0 ? (1, 0, 0) : (-1, 0, 0); break; }
                            case '|': { int v = ip.StackStack.Pop(); ip.Delta = v == 0 ? (0, 1, 0) : (0, -1, 0); break; }
                            case '[': ip.Delta = (ip.Delta.Y, -ip.Delta.X, 0); break;
                            case ']': ip.Delta = (-ip.Delta.Y, ip.Delta.X, 0); break;
                            case 'r': ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break;
                            case 'x': { var v = PopVector(ip.StackStack); ip.Delta = v; break; }
                            case 'w': { int b = ip.StackStack.Pop(); int a = ip.StackStack.Pop(); if (a > b) ip.Delta = (-ip.Delta.Y, ip.Delta.X, 0); else if (a < b) ip.Delta = (ip.Delta.Y, -ip.Delta.X, 0); break; }
                            case '#': ip.Position = Advance(ip.Position, ip.Delta); break;
                            case 'j': { int s = ip.StackStack.Pop(); var step = s >= 0 ? ip.Delta : (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); int count = Math.Abs(s); for (int i = 0; i < count; i++) ip.Position = Advance(ip.Position, step); suppressAdvance = true; break; }
                            case ';': { ip.Position = Advance(ip.Position, ip.Delta); while (GetCell(ip.Position.X, ip.Position.Y, ip.Position.Z) != ';') ip.Position = Advance(ip.Position, ip.Delta); break; }
                            case '\'': { ip.Position = Advance(ip.Position, ip.Delta); ip.StackStack.Push(GetCell(ip.Position.X, ip.Position.Y, ip.Position.Z)); break; }
                            case 's': { int sv = ip.StackStack.Pop(); ip.Position = Advance(ip.Position, ip.Delta); SetCell(ip.Position.X, ip.Position.Y, ip.Position.Z, sv); break; }
                            case 'g': { int z = ip.StackStack.Pop(); int y = ip.StackStack.Pop(); int x = ip.StackStack.Pop(); ip.StackStack.Push(GetCell(x + ip.Offset.X, y + ip.Offset.Y, z + ip.Offset.Z)); break; }
                            case 'p': { int z = ip.StackStack.Pop(); int y = ip.StackStack.Pop(); int x = ip.StackStack.Pop(); int v = ip.StackStack.Pop(); SetCell(x + ip.Offset.X, y + ip.Offset.Y, z + ip.Offset.Z, v); break; }
                            case '.': if (!hasOutput) throw new InvalidOperationException("Output '.' without an output interface"); writeOutputInt(ip.StackStack.Pop()); writeOutputChar(' '); break;
                            case ',': if (!hasOutput) throw new InvalidOperationException("Output ',' without an output interface"); writeOutputChar(ip.StackStack.Pop()); break;
                            case '&': if (!hasInput) throw new InvalidOperationException("Input '&' without an input interface"); var line = input.ReadLine(); if (line == null) ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); else { int v; ip.StackStack.Push(int.TryParse(line.Trim(), out v) ? v : 0); } break;
                            case '~': if (!hasInput) throw new InvalidOperationException("Input '~' without an input interface"); int ch = input.Read(); if (ch < 0) ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); else ip.StackStack.Push(ch); break;
                            case 'i': { if (!TryPopZeroTerminatedString(ip.StackStack, out var fileName)) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; } int flags = ip.StackStack.Pop(); var va = PopVector(ip.StackStack); var actualVa = (va.X + ip.Offset.X, va.Y + ip.Offset.Y, va.Z + ip.Offset.Z); if (!TryInputFile(actualVa, fileName, (flags & 1) != 0, out var vb)) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; } PushVector(ip.StackStack, va); PushVector(ip.StackStack, vb); break; }
                            case 'o': { if (!TryPopZeroTerminatedString(ip.StackStack, out var fileName)) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; } int flags = ip.StackStack.Pop(); var vb = PopVector(ip.StackStack); var va = PopVector(ip.StackStack); var actualVa = (va.X + ip.Offset.X, va.Y + ip.Offset.Y, va.Z + ip.Offset.Z); if (!TryOutputFile(actualVa, vb, fileName, (flags & 1) != 0)) ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; }
                            case '@': ip.IsStopped = true; break;
                            case 'q': exitCode = ip.StackStack.Pop(); quit = true; break;
                            case 'k':
                                {
                                    int n = ip.StackStack.Pop();
                                    var instrPos = Advance(ip.Position, ip.Delta);
                                    while (true) { int c = GetCell(instrPos.X, instrPos.Y, instrPos.Z); if (c == ' ' || c == '\t' || c == '\f' || c == '\v') instrPos = Advance(instrPos, ip.Delta); else if (c == ';') { instrPos = Advance(instrPos, ip.Delta); while (GetCell(instrPos.X, instrPos.Y, instrPos.Z) != ';') instrPos = Advance(instrPos, ip.Delta); instrPos = Advance(instrPos, ip.Delta); } else break; }
                                    if (n == 0) ip.Position = instrPos;
                                    else { int operand = GetCell(instrPos.X, instrPos.Y, instrPos.Z); for (int i = 0; i < n && !ip.IsStopped && !quit; i++) { bool dummy = false; ExecuteInstruction(ip, ipNode, ref dummy, operand); } }
                                    break;
                                }
                            case 't': { var child = ip.CreateChild(ips.Count); ips.AddAfter(ipNode, child); break; }
                            case '{':
                                {
                                    int n = ip.StackStack.Pop();
                                    var items = new List<int>();
                                    if (n > 0) for (int i = 0; i < n; i++) items.Add(ip.StackStack.Pop());
                                    ip.StackStack.Push(ip.Offset.X); ip.StackStack.Push(ip.Offset.Y); ip.StackStack.Push(ip.Offset.Z);
                                    ip.StackStack.PushNewStack();
                                    if (n > 0) for (int i = items.Count - 1; i >= 0; i--) ip.StackStack.Push(items[i]);
                                    else if (n < 0) { var soss = ip.StackStack.SOSS; if (soss != null) for (int i = 0; i < -n; i++) soss.Push(0); }
                                    ip.Offset = Advance(ip.Position, ip.Delta);
                                    break;
                                }
                            case '}':
                                {
                                    int n = ip.StackStack.Pop();
                                    if (!ip.StackStack.HasSOSS) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; }
                                    var items = new List<int>();
                                    for (int i = 0; i < Math.Max(0, n); i++) items.Add(ip.StackStack.Pop());
                                    ip.StackStack.PopCurrentStack();
                                    int oz = ip.StackStack.Pop(), oy = ip.StackStack.Pop(), ox = ip.StackStack.Pop();
                                    ip.Offset = (ox, oy, oz);
                                    if (n < 0) for (int i = 0; i < -n; i++) ip.StackStack.Pop();
                                    for (int i = items.Count - 1; i >= 0; i--) ip.StackStack.Push(items[i]);
                                    break;
                                }
                            case 'u':
                                {
                                    int n = ip.StackStack.Pop();
                                    if (!ip.StackStack.HasSOSS) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; }
                                    var soss = ip.StackStack.SOSS!;
                                    if (n > 0) for (int i = 0; i < n; i++) ip.StackStack.Push(soss.Count > 0 ? soss.Pop() : 0);
                                    else if (n < 0) for (int i = 0; i < -n; i++) soss.Push(ip.StackStack.Pop());
                                    break;
                                }
                            case 'y': { int c = ip.StackStack.Pop(); PushSysInfo(ip, ips.Count, c); break; }
                            case '(': case ')': { int n = ip.StackStack.Pop(); for (int i = 0; i < n; i++) ip.StackStack.Pop(); ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; }
                            case '=': { if (!TryPopZeroTerminatedString(ip.StackStack, out var cmd)) { ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break; } ip.StackStack.Push(ExecuteSystemCommand(cmd)); break; }
                            default: if (cell >= 'A' && cell <= 'Z') ip.Delta = (-ip.Delta.X, -ip.Delta.Y, -ip.Delta.Z); break;
                        }
                    }

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
        """;
}
