using Esolang.Funge.Parser;
using Esolang.Processor;
using System.Collections;
using System.Diagnostics;

namespace Esolang.Funge.Processor;

/// <summary>
/// Executes a Funge-98 program loaded into a <see cref="FungeSpace"/>.
/// Supports the full core instruction set including concurrent IPs (<c>t</c>) and
/// the stack stack (<c>{</c>/<c>}</c>/<c>u</c>).
/// Fingerprints (<c>(</c>/<c>)</c>) reflect (not implemented).
/// Includes Trefunge 3-D direction instructions (<c>h</c>/<c>l</c>/<c>m</c>).
/// </summary>
/// <remarks>
/// Initializes a new <see cref="FungeProcessor"/> with the given program space and optional I/O.
/// </remarks>
/// <param name="space">The parsed Funge-98 program space.</param>
/// <param name="output">Output writer; defaults to <see cref="Console.Out"/>.</param>
/// <param name="input">Input reader; defaults to <see cref="Console.In"/>.</param>
/// <param name="commandLineArguments">Optional command-line arguments exposed by <c>y</c>. Defaults to host process args.</param>
/// <param name="environmentVariables">Optional environment variable entries (<c>NAME=VALUE</c>) exposed by <c>y</c>. Defaults to host process environment.</param>
public sealed partial class FungeProcessor(
    FungeSpace space,
    TextWriter? output = null,
    TextReader? input = null,
    IEnumerable<string>? commandLineArguments = null,
    IEnumerable<string>? environmentVariables = null) : ITextProcessor<FungeSpace>
{
    private readonly FungeSpace _space = space;
    private readonly TextWriter _output = output ?? Console.Out;
    private readonly TextReader _input = input ?? Console.In;
    private readonly string[] _commandLineArguments = (commandLineArguments ?? Environment.GetCommandLineArgs())
#pragma warning disable IDE0305 // コレクションの初期化を簡略化します
            .ToArray();
#pragma warning restore IDE0305 // コレクションの初期化を簡略化します
    private readonly string[] _environmentVariables = [.. environmentVariables
             ?? Environment.GetEnvironmentVariables()
            .Cast<DictionaryEntry>()
            .Select(static entry => $"{entry.Key}={entry.Value}")];
    private readonly Random _random = new();
    private int _nextIpId;

    /// <inheritdoc/>
    public FungeSpace Program => _space;

    /// <summary>
    /// Runs the Funge-98 program and returns the process exit code.
    /// The program starts with a single IP at (0,0) moving East.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>Exit code: 0 unless the program used <c>q</c>.</returns>
    public int Run(CancellationToken cancellationToken = default)
        => RunToEnd(null, null, cancellationToken);

    /// <inheritdoc/>
    public int RunToEnd(TextReader? input = null, TextWriter? output = null, CancellationToken cancellationToken = default)
    {
        var resolvedInput = input ?? _input;
        var resolvedOutput = output ?? _output;

        var ips = new LinkedList<InstructionPointer>();
        ips.AddFirst(new InstructionPointer(_nextIpId++));
        var exitCode = 0;
        var quit = false;

        while (ips.Count > 0 && !quit && !cancellationToken.IsCancellationRequested)
        {
            var node = ips.First!;
            while (node is not null && !quit && !cancellationToken.IsCancellationRequested)
            {
                var nextNode = node.Next;
                var ip = node.Value;

                var suppressAdvance = false;
                ExecuteInstruction(ip, ips, node, ref exitCode, ref quit, ref suppressAdvance, resolvedInput, resolvedOutput);

                if (ip.IsStopped || quit)
                {
                    ips.Remove(node);
                }
                else if (!suppressAdvance)
                {
                    ip.Position = _space.Advance(ip.Position, ip.Delta);
                }

                node = nextNode;
            }
        }

        return exitCode;
    }

    /// <inheritdoc/>
    public ValueTask<int> RunToEndAsync(TextReader? input = null, TextWriter? output = null, CancellationToken cancellationToken = default)
        => ValueTask.FromResult(RunToEnd(input, output, cancellationToken));

    private void ExecuteInstruction(
        InstructionPointer ip,
        LinkedList<InstructionPointer> ips,
        LinkedListNode<InstructionPointer> ipNode,
        ref int exitCode,
        ref bool quit,
        ref bool suppressAdvance,
        TextReader input,
        TextWriter output,
        int? overrideCell = null)
    {
        var cell = overrideCell ?? _space[ip.Position];

        // String mode: push each character until closing "
        if (ip.StringMode)
        {
            if (cell == '"')
            {
                ip.StringMode = false;
            }
            else if (cell is ' ' or '\t' or '\f' or '\v')
            {
                // Funge-98 stringmode treats contiguous spaces SGML-style:
                // one pushed space, one tick.
                ip.StackStack.Push(' ');
                while (true)
                {
                    var next = _space.Advance(ip.Position, ip.Delta);
                    var nextCell = _space[next];
                    if (nextCell is ' ' or '\t' or '\f' or '\v')
                    {
                        ip.Position = next;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else
            {
                ip.StackStack.Push(cell);
            }
            return;
        }

        switch (cell)
        {
            // ── No-ops ──────────────────────────────────────────────────────
            case ' ': // Space: no-op (IP passes through)
            case '\t': // SGML space: tab
            case '\f': // SGML space: form feed
            case '\v': // SGML space: vertical tab
            case 'z': // z: explicit no-op
                break;

            // ── Stack manipulation ───────────────────────────────────────────
            case '!': // Logical Not
                ip.StackStack.Push(ip.StackStack.Pop() == 0 ? 1 : 0);
                break;

            case '$': // Pop
                ip.StackStack.Pop();
                break;

            case ':': // Duplicate
                {
                    var v = ip.StackStack.Pop();
                    ip.StackStack.Push(v);
                    ip.StackStack.Push(v);
                    break;
                }

            case '\\': // Swap
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(b);
                    ip.StackStack.Push(a);
                    break;
                }

            case 'n': // Clear Stack
                ip.StackStack.ClearToss();
                break;

            // ── Arithmetic ───────────────────────────────────────────────────
            case '+':
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(a + b);
                    break;
                }

            case '-':
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(a - b);
                    break;
                }

            case '*':
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(a * b);
                    break;
                }

            case '/':
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(b == 0 ? 0 : a / b);
                    break;
                }

            case '%': // Remainder
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(b == 0 ? 0 : a % b);
                    break;
                }

            case '`': // Greater Than
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    ip.StackStack.Push(a > b ? 1 : 0);
                    break;
                }

            // ── Digit/hex pushers ────────────────────────────────────────────
            case '0' or '1' or '2' or '3' or '4'
              or '5' or '6' or '7' or '8' or '9':
                ip.StackStack.Push(cell - '0');
                break;

            case 'a': ip.StackStack.Push(10); break;
            case 'b': ip.StackStack.Push(11); break;
            case 'c': ip.StackStack.Push(12); break;
            case 'd': ip.StackStack.Push(13); break;
            case 'e': ip.StackStack.Push(14); break;
            case 'f': ip.StackStack.Push(15); break;

            // ── Direction ───────────────────────────────────────────────────
            case '>': ip.Delta = FungeVector.East; break;
            case '<': ip.Delta = FungeVector.West; break;
            case '^': ip.Delta = FungeVector.North; break;
            case 'v': ip.Delta = FungeVector.South; break;

            case '?': // Go Away: random cardinal direction
                ip.Delta = _random.Next(6) switch
                {
                    0 => FungeVector.East,
                    1 => FungeVector.West,
                    2 => FungeVector.North,
                    3 => FungeVector.South,
                    4 => FungeVector.High,
                    _ => FungeVector.Low,
                };
                break;

            case 'h': // Go High (3-D)
                ip.Delta = FungeVector.High;
                break;

            case 'l': // Go Low (3-D)
                ip.Delta = FungeVector.Low;
                break;

            case 'm': // High-Low If (3-D)
                ip.Delta = ip.StackStack.Pop() == 0 ? FungeVector.Low : FungeVector.High;
                break;

            case '_': // East-West If
                ip.Delta = ip.StackStack.Pop() == 0 ? FungeVector.East : FungeVector.West;
                break;

            case '|': // North-South If
                ip.Delta = ip.StackStack.Pop() == 0 ? FungeVector.South : FungeVector.North;
                break;

            case '[': // Turn Left (CCW 90°)
                ip.Delta = ip.Delta.RotateLeft();
                break;

            case ']': // Turn Right (CW 90°)
                ip.Delta = ip.Delta.RotateRight();
                break;

            case 'r': // Reflect
                ip.Delta = ip.Delta.Reflect();
                break;

            case 'x': // Absolute Delta
                {
                    int dz = ip.StackStack.Pop(), dy = ip.StackStack.Pop(), dx = ip.StackStack.Pop();
                    ip.Delta = new FungeVector(dx, dy, dz);
                    break;
                }

            case 'w': // Compare
                {
                    int b = ip.StackStack.Pop(), a = ip.StackStack.Pop();
                    if (a > b) ip.Delta = ip.Delta.RotateRight();
                    else if (a < b) ip.Delta = ip.Delta.RotateLeft();
                    // a == b: no change (acts as 'z')
                    break;
                }

            // ── Movement modifiers ───────────────────────────────────────────
            case '#': // Trampoline: skip next cell
                ip.Position = _space.Advance(ip.Position, ip.Delta);
                break;

            case 'j': // Jump Forward s cells (suppressAdvance: sets position directly)
                {
                    var s = ip.StackStack.Pop();
                    var dir = s >= 0 ? ip.Delta : ip.Delta.Reflect();
                    for (var i = 0; i < Math.Abs(s); i++)
                        ip.Position = _space.Advance(ip.Position, dir);
                    suppressAdvance = true;
                    break;
                }

            case ';': // Jump Over: skip until next ;
                ip.Position = _space.Advance(ip.Position, ip.Delta);
                while (_space[ip.Position] != ';')
                    ip.Position = _space.Advance(ip.Position, ip.Delta);
                break;

            // ── Character fetch/store ────────────────────────────────────────
            case '\'': // Fetch Character: push value of next cell, skip it
                ip.Position = _space.Advance(ip.Position, ip.Delta);
                ip.StackStack.Push(_space[ip.Position]);
                break;

            case 's': // Store Character: store to next cell, skip it
                {
                    var val = ip.StackStack.Pop();
                    ip.Position = _space.Advance(ip.Position, ip.Delta);
                    _space[ip.Position] = val;
                    break;
                }

            // ── String mode ──────────────────────────────────────────────────
            case '"': // Toggle Stringmode
                ip.StringMode = true;
                break;

            // ── FungeSpace get/put ───────────────────────────────────────────
            case 'g': // Get: read cell at (x+offset, y+offset, z+offset)
                {
                    int z = ip.StackStack.Pop(), y = ip.StackStack.Pop(), x = ip.StackStack.Pop();
                    ip.StackStack.Push(_space[new FungeVector(x + ip.Offset.X, y + ip.Offset.Y, z + ip.Offset.Z)]);
                    break;
                }

            case 'p': // Put: write cell at (x+offset, y+offset, z+offset)
                {
                    int z = ip.StackStack.Pop(), y = ip.StackStack.Pop(), x = ip.StackStack.Pop();
                    var val = ip.StackStack.Pop();
                    _space[new FungeVector(x + ip.Offset.X, y + ip.Offset.Y, z + ip.Offset.Z)] = val;
                    break;
                }

            // ── I/O ──────────────────────────────────────────────────────────
            case '.': // Output Integer
                output.Write(ip.StackStack.Pop());
                output.Write(' ');
                break;

            case ',': // Output Character
                output.Write((char)ip.StackStack.Pop());
                break;

            case '&': // Input Integer
                {
                    var line = input.ReadLine();
                    if (line is null) { ip.Delta = ip.Delta.Reflect(); break; }
                    ip.StackStack.Push(int.TryParse(line.Trim(), out var v) ? v : 0);
                    break;
                }

            case '~': // Input Character
                {
                    var ch = input.Read();
                    if (ch < 0) ip.Delta = ip.Delta.Reflect();
                    else ip.StackStack.Push(ch);
                    break;
                }

            case 'i': // Input File
                {
                    if (!TryPopZeroTerminatedString(ip.StackStack, out var fileName))
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }

                    var flags = ip.StackStack.Pop();
                    var va = PopVector(ip.StackStack) + ip.Offset;
                    var binaryMode = (flags & 1) != 0;

                    if (!TryInputFile(va, fileName, binaryMode, out var vb))
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }

                    PushVector(ip.StackStack, va - ip.Offset);
                    PushVector(ip.StackStack, vb);
                    break;
                }

            case 'o': // Output File
                {
                    if (!TryPopZeroTerminatedString(ip.StackStack, out var fileName))
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }

                    var flags = ip.StackStack.Pop();
                    var vb = PopVector(ip.StackStack);
                    var va = PopVector(ip.StackStack) + ip.Offset;
                    var linearText = (flags & 1) != 0;

                    if (!TryOutputFile(va, vb, fileName, linearText))
                        ip.Delta = ip.Delta.Reflect();
                    break;
                }

            // ── Control flow ─────────────────────────────────────────────────
            case '@': // Stop this IP
                ip.IsStopped = true;
                break;

            case 'q': // Quit program immediately
                exitCode = ip.StackStack.Pop();
                quit = true;
                break;

            case 'k': // Iterate: execute next instruction n times
                {
                    var n = ip.StackStack.Pop();

                    // Advance past spaces AND semicolon-delimited sections to find the operand.
                    // Per spec, k skips spaces and ';'-enclosed regions just as normal execution would.
                    var instrPos = _space.Advance(ip.Position, ip.Delta);
                    while (true)
                    {
                        var c = _space[instrPos];
                        if (c is ' ' or '\t' or '\f' or '\v')
                        {
                            instrPos = _space.Advance(instrPos, ip.Delta);
                        }
                        else if (c == ';')
                        {
                            // skip the semicolon section entirely (same as the ';' instruction)
                            instrPos = _space.Advance(instrPos, ip.Delta);
                            while (_space[instrPos] != ';')
                                instrPos = _space.Advance(instrPos, ip.Delta);
                            instrPos = _space.Advance(instrPos, ip.Delta);
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (n == 0)
                    {
                        // n=0: skip the operand. IP moves to instrPos, then normal advance passes it.
                        ip.Position = instrPos;
                    }
                    else
                    {
                        // n>0: execute the discovered operand n times, but execute it AT k.
                        // The operand is only searched for; its semantics apply at the current IP position.
                        // After k finishes, normal advancement continues from the IP's current position,
                        // so position-changing operands such as [ and # behave "from k".
                        var operand = _space[instrPos];
                        for (var i = 0; i < n && !ip.IsStopped && !quit; i++)
                        {
                            var dummy = false;
                            ExecuteInstruction(ip, ips, ipNode, ref exitCode, ref quit, ref dummy, input, output, operand);
                        }
                    }
                    break;
                }

            // ── Concurrency ──────────────────────────────────────────────────
            case 't': // Split: create child IP with reflected delta
                {
                    var child = ip.CreateChild(_nextIpId++);
                    ips.AddAfter(ipNode, child);
                    break;
                }

            // ── Stack Stack operations ────────────────────────────────────────
            case '{': // Begin Block
                {
                    var n = ip.StackStack.Pop();

                    // Collect n items from TOSS (top item first)
                    var items = new List<int>();
                    if (n > 0)
                        for (var i = 0; i < n; i++) items.Add(ip.StackStack.Pop());

                    // Push storage offset to current TOSS (will become SOSS)
                    ip.StackStack.Push(ip.Offset.X);
                    ip.StackStack.Push(ip.Offset.Y);
                    ip.StackStack.Push(ip.Offset.Z);

                    // Push new empty stack (old TOSS becomes SOSS)
                    ip.StackStack.PushNewStack();

                    if (n > 0)
                    {
                        // Re-push items so original top is on top of new TOSS
                        for (var i = items.Count - 1; i >= 0; i--)
                            ip.StackStack.Push(items[i]);
                    }
                    else if (n < 0)
                    {
                        // Push |n| zeros to SOSS
                        var soss = ip.StackStack.SOSS!;
                        for (var i = 0; i < -n; i++) soss.Push(0);
                    }

                    // Set storage offset to next cell position
                    ip.Offset = _space.Advance(ip.Position, ip.Delta);
                    break;
                }

            case '}': // End Block
                {
                    var n = ip.StackStack.Pop();
                    if (!ip.StackStack.HasSOSS)
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }

                    // Collect items from TOSS
                    var items = new List<int>();
                    for (var i = 0; i < Math.Max(0, n); i++) items.Add(ip.StackStack.Pop());

                    // Pop current TOSS (discard remaining items)
                    ip.StackStack.PopCurrentStack();

                    // Restore storage offset (Z on top, then Y, then X)
                    var oz = ip.StackStack.Pop();
                    var oy = ip.StackStack.Pop();
                    var ox = ip.StackStack.Pop();
                    ip.Offset = new FungeVector(ox, oy, oz);

                    // If n < 0, discard |n| items from (now current) TOSS
                    if (n < 0)
                        for (var i = 0; i < -n; i++) ip.StackStack.Pop();

                    // Push collected items (original top on top)
                    for (var i = items.Count - 1; i >= 0; i--)
                        ip.StackStack.Push(items[i]);
                    break;
                }

            case 'u': // Stack Under Stack
                {
                    var n = ip.StackStack.Pop();
                    if (!ip.StackStack.HasSOSS)
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }
                    var soss = ip.StackStack.SOSS!;
                    if (n > 0)
                        for (var i = 0; i < n; i++) ip.StackStack.Push(soss.Count > 0 ? soss.Pop() : 0);
                    else if (n < 0)
                        for (var i = 0; i < -n; i++) soss.Push(ip.StackStack.Pop());
                    break;
                }

            // ── System info ──────────────────────────────────────────────────
            case 'y': // Get SysInfo
                {
                    var c = ip.StackStack.Pop();
                    PushSysInfo(ip, ips.Count, c);
                    break;
                }

            // ── Fingerprints (reflect – not implemented) ─────────────────────
            case '(': // Load Semantics
                {
                    var n = ip.StackStack.Pop();
                    for (var i = 0; i < n; i++) ip.StackStack.Pop();
                    ip.Delta = ip.Delta.Reflect();
                    break;
                }

            case ')': // Unload Semantics
                {
                    var n = ip.StackStack.Pop();
                    for (var i = 0; i < n; i++) ip.StackStack.Pop();
                    ip.Delta = ip.Delta.Reflect();
                    break;
                }

            // ── Optional (reflect) ────────────────────────────────────────────
            case '=': // Execute (system exec)
                {
                    if (!TryPopZeroTerminatedString(ip.StackStack, out var command))
                    {
                        ip.Delta = ip.Delta.Reflect();
                        break;
                    }

                    ip.StackStack.Push(ExecuteSystemCommand(command));
                    break;
                }

            default:
                // A-Z: fingerprint-defined; reflect if not loaded
                if (cell is >= 'A' and <= 'Z')
                    ip.Delta = ip.Delta.Reflect();
                // All other characters: no-op
                break;
        }
    }

    private static FungeVector PopVector(StackStack stack)
    {
        var z = stack.Pop();
        var y = stack.Pop();
        var x = stack.Pop();
        return new FungeVector(x, y, z);
    }

    private static void PushVector(StackStack stack, FungeVector vector)
    {
        stack.Push(vector.X);
        stack.Push(vector.Y);
        stack.Push(vector.Z);
    }

    private static bool TryPopZeroTerminatedString(StackStack stack, out string result)
    {
        var chars = new List<char>();
        while (true)
        {
            var value = stack.Pop();
            if (value == 0)
            {
                result = new string([.. chars]);
                return true;
            }

            if (value is < char.MinValue or > char.MaxValue)
            {
                result = string.Empty;
                return false;
            }

            chars.Add((char)value);
        }
    }

    private bool TryInputFile(FungeVector leastPoint, string fileName, bool binaryMode, out FungeVector size)
    {
        size = new FungeVector(0, 0, 0);

        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(fileName);
        }
        catch
        {
            return false;
        }

        var x = 0;
        var y = 0;
        var z = 0;
        var wroteAny = false;
        var maxX = 0;
        var maxY = 0;
        var maxZ = 0;

        foreach (var raw in bytes)
        {
            var cell = (int)raw;

            if (!binaryMode)
            {
                if (cell == '\r')
                    continue;
                if (cell == '\n')
                {
                    x = 0;
                    y++;
                    continue;
                }
                if (cell == '\f')
                {
                    x = 0;
                    y = 0;
                    z++;
                    continue;
                }
                if (cell is '\t' or '\v')
                    cell = ' ';
            }

            var pos = new FungeVector(leastPoint.X + x, leastPoint.Y + y, leastPoint.Z + z);
            _space.EnsureBounds(pos);

            if (binaryMode || cell != ' ')
                _space[pos] = cell;

            wroteAny = true;
            if (x > maxX) maxX = x;
            if (y > maxY) maxY = y;
            if (z > maxZ) maxZ = z;
            x++;
        }

        size = wroteAny ? new FungeVector(maxX, maxY, maxZ) : new FungeVector(0, 0, 0);
        return true;
    }

    private bool TryOutputFile(FungeVector leastPoint, FungeVector size, string fileName, bool linearText)
    {
        var sx = Math.Max(0, size.X);
        var sy = Math.Max(0, size.Y);
        var sz = Math.Max(0, size.Z);

        var rows = new List<string>();
        for (var z = 0; z <= sz; z++)
        {
            for (var y = 0; y <= sy; y++)
            {
                var chars = new char[sx + 1];
                for (var x = 0; x <= sx; x++)
                {
                    var c = _space[new FungeVector(leastPoint.X + x, leastPoint.Y + y, leastPoint.Z + z)];
                    chars[x] = c is >= char.MinValue and <= char.MaxValue ? (char)c : ' ';
                }

                var row = new string(chars);
                rows.Add(linearText ? row.TrimEnd(' ') : row);
            }

            if (z != sz)
                rows.Add("\f");
        }

        if (linearText)
        {
            while (rows.Count > 0 && rows[^1].Length == 0)
                rows.RemoveAt(rows.Count - 1);
        }

        var text = string.Join("\n", rows);
        var bytes = text.Select(static ch => (byte)(ch & 0xFF)).ToArray();

        try
        {
            File.WriteAllBytes(fileName, bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static int ExecuteSystemCommand(string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
                CreateNoWindow = true,
            };

            if (OperatingSystem.IsWindows())
            {
                processStartInfo.FileName = "cmd.exe";
                processStartInfo.ArgumentList.Add("/c");
                processStartInfo.ArgumentList.Add(command);
            }
            else
            {
                processStartInfo.FileName = "/bin/sh";
                processStartInfo.ArgumentList.Add("-c");
                processStartInfo.ArgumentList.Add(command);
            }

            using var process = Process.Start(processStartInfo);
            if (process is null)
                return -1;

            process.WaitForExit();
            return process.ExitCode;
        }
        catch
        {
            return -1;
        }
    }

    /// <summary>
    /// Pushes system information onto the TOSS of the given IP.
    /// If <paramref name="c"/> is greater than zero, only item <paramref name="c"/>
    /// (1-indexed from top) is left on the stack.
    /// </summary>
    private void PushSysInfo(InstructionPointer ip, int _, int c)
    {
        // Build list of items in order: items[0] will be last-pushed (item 1 from top)
        List<int> items = [];

        // 1. Flags: /t + /i + /o + /= supported
        items.Add(0x01 | 0x02 | 0x04 | 0x08);
        // 2. Cell size in bytes
        items.Add(4);
        // 3. Interpreter handprint ("Fung" as big-endian int)
        items.Add(unchecked((int)0x46756E67u));
        // 4. Version (Funge-98 = 9800)
        items.Add(9800);
        // 5. Operating paradigm (1 = equivalent to C system() behavior)
        items.Add(1);
        // 6. Path separator
        items.Add(Path.DirectorySeparatorChar);
        // 7. Number of dimensions (3 = Trefunge)
        items.Add(3);
        // 8. IP unique ID
        items.Add(ip.Id);
        // 9. IP team number
        items.Add(0);
        // 10-12. IP position (X, Y, Z; Z on top)
        items.Add(ip.Position.X);
        items.Add(ip.Position.Y);
        items.Add(ip.Position.Z);
        // 13-15. IP delta (dX, dY, dZ; dZ on top)
        items.Add(ip.Delta.X);
        items.Add(ip.Delta.Y);
        items.Add(ip.Delta.Z);
        // 16-18. Storage offset (oX, oY, oZ; oZ on top)
        items.Add(ip.Offset.X);
        items.Add(ip.Offset.Y);
        items.Add(ip.Offset.Z);
        // 19-21. Least point of LSAB (minX, minY, minZ; minZ on top)
        items.Add(_space.MinX);
        items.Add(_space.MinY);
        items.Add(_space.MinZ);
        // 22-24. Greatest point relative to least point (max-min)
        items.Add(_space.MaxX - _space.MinX);
        items.Add(_space.MaxY - _space.MinY);
        items.Add(_space.MaxZ - _space.MinZ);

        var now = DateTime.Now;
        // 20. Current date: (year-1900)*256*256 + month*256 + day
        items.Add(((now.Year - 1900) * 256 * 256) + (now.Month * 256) + now.Day);
        // 21. Current time: HH*256*256 + MM*256 + SS
        items.Add((now.Hour * 256 * 256) + (now.Minute * 256) + now.Second);
        // 22. Number of stacks in stack stack
        items.Add(ip.StackStack.StackCount);
        // 23+. Size of each stack (TOSS first)
        foreach (var stack in ip.StackStack.AllStacks)
            items.Add(stack.Count);
        // Command-line args: null-terminated strings, series terminated by extra null.
        foreach (var arg in _commandLineArguments)
        {
            foreach (var ch in arg)
                items.Add(ch);
            items.Add(0);
        }
        // Series terminator
        items.Add(0);

        // Environment variables: null-terminated strings, series terminated by extra null.
        foreach (var env in _environmentVariables)
        {
            foreach (var ch in env)
                items.Add(ch);
            items.Add(0);
        }
        // Series terminator
        items.Add(0);

        // Push in reverse order so items[0] ends up on top (= item 1)
        for (var i = items.Count - 1; i >= 0; i--)
            ip.StackStack.Push(items[i]);

        if (c > 0)
        {
            // y with positive c: keep only the c-th item from the top of the full stack.
            // This naturally allows "pick" behavior when c exceeds y's own payload length.
            var snapshot = ip.StackStack.TOSS.ToArray(); // top-first
            var picked = c <= snapshot.Length ? snapshot[c - 1] : 0;

            for (var i = 0; i < items.Count; i++)
                ip.StackStack.Pop();

            ip.StackStack.Push(picked);
        }
    }
}
