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
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static int RunSync(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                    => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken);
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunString) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static string RunString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                {
                    using var output = new StringWriter();
                    RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken);
                    return output.ToString();
                }
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunEnumerable) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static IEnumerable<byte> RunEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                {
                    foreach (var b in RunCoreEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, cancellationToken))
                        yield return b;
                }
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunAsyncEnumerable) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static async IAsyncEnumerable<byte> RunAsyncEnumerable(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, [EnumeratorCancellation] CancellationToken cancellationToken = default)
                {
                    await foreach (var b in RunCoreAsyncEnumerable(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, cancellationToken))
                        yield return b;
                }
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunTask) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task RunTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken), cancellationToken);
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunTaskInt) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<int> RunTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                    => Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken), cancellationToken);
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunTaskString) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static Task<string> RunTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                {
                    return Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken);
                        return output.ToString();
                    }, cancellationToken);
                }
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunValueTask) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask RunValueTask(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken), cancellationToken));
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunValueTaskInt) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<int> RunValueTaskInt(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, TextWriter output, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                    => new(Task.Run(() => RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken), cancellationToken));
        """);
        }

        if ((features & RuntimeFacadeFeatures.RunValueTaskString) != 0)
        {
            sb.Append("""
                [global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]
                internal static ValueTask<string> RunValueTaskString(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, CancellationToken cancellationToken = default)
                {
                    return new ValueTask<string>(Task.Run(() =>
                    {
                        using var output = new StringWriter();
                        RunCore(cells, minX, minY, minZ, maxX, maxY, maxZ, input, output, hasInput, hasOutput, cancellationToken);
                        return output.ToString();
                    }, cancellationToken));
                }
        """);
        }

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
                    
                    var runTask = Task.Run(() => {
                        try {
                            Execute(cells, minX, minY, minZ, maxX, maxY, maxZ, input, hasInput, hasOutput, (byte b) => buffer.Enqueue(b), ct);
                            tcs.SetResult(0);
                        } catch (Exception ex) { tcs.SetException(ex); }
                    }, ct);

                    while (!tcs.Task.IsCompleted || !buffer.IsEmpty)
                    {
                        while (buffer.TryDequeue(out var b))
                        {
                            yield return b;
                        }
                        if (tcs.Task.IsCompleted) break;
                        await Task.Delay(10, ct);
                    }
                    await tcs.Task;
                }

                private static int Execute(Dictionary<(int, int, int), int> cells, int minX, int minY, int minZ, int maxX, int maxY, int maxZ, TextReader input, bool hasInput, bool hasOutput, Action<byte> writeOutput, CancellationToken ct)
                {
                    return 0; // Logic migration pending
                }
            }
        }
        """);
}
