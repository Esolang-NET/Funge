using Esolang.Funge.Parser;

namespace Esolang.Funge.Processor;

/// <summary>
/// Executes a Funge-98 program loaded into a <see cref="FungeSpace"/>.
/// Supports the full core instruction set including concurrent IPs (<c>t</c>) and
/// the stack stack (<c>{</c>/<c>}</c>/<c>u</c>).
/// Fingerprints (<c>(</c>/<c>)</c>) and file I/O (<c>i</c>/<c>o</c>) reflect (not implemented).
/// 3-D instructions (<c>h</c>/<c>l</c>/<c>m</c>) reflect in 2-D mode.
/// </summary>
public sealed class FungeProcessor
{
    private readonly FungeSpace _space;
    private readonly TextWriter _output;
    private readonly TextReader _input;
    private readonly Random _random = new();
    private int _nextIpId;

    /// <summary>
    /// Initializes a new <see cref="FungeProcessor"/> with the given program space and optional I/O.
    /// </summary>
    /// <param name="space">The parsed Funge-98 program space.</param>
    /// <param name="output">Output writer; defaults to <see cref="Console.Out"/>.</param>
    /// <param name="input">Input reader; defaults to <see cref="Console.In"/>.</param>
    public FungeProcessor(FungeSpace space, TextWriter? output = null, TextReader? input = null)
    {
        _space = space;
        _output = output ?? Console.Out;
        _input = input ?? Console.In;
    }

    /// <summary>
    /// Runs the Funge-98 program and returns the process exit code.
    /// The program starts with a single IP at (0,0) moving East.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>Exit code: 0 unless the program used <c>q</c>.</returns>
    public int Run(CancellationToken cancellationToken = default)
    {
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
                ExecuteInstruction(ip, ips, node, ref exitCode, ref quit, ref suppressAdvance);

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

    private void ExecuteInstruction(
        InstructionPointer ip,
        LinkedList<InstructionPointer> ips,
        LinkedListNode<InstructionPointer> ipNode,
        ref int exitCode,
        ref bool quit,
        ref bool suppressAdvance)
    {
        var cell = _space[ip.Position];

        // String mode: push each character until closing "
        if (ip.StringMode)
        {
            if (cell == '"')
                ip.StringMode = false;
            else
                ip.StackStack.Push(cell);
            return;
        }

        switch (cell)
        {
            // ── No-ops ──────────────────────────────────────────────────────
            case ' ': // Space: no-op (IP passes through)
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
                ip.Delta = _random.Next(4) switch
                {
                    0 => FungeVector.East,
                    1 => FungeVector.West,
                    2 => FungeVector.North,
                    _ => FungeVector.South,
                };
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
                int dy = ip.StackStack.Pop(), dx = ip.StackStack.Pop();
                ip.Delta = new FungeVector(dx, dy);
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
            case 'g': // Get: read cell at (x+offset, y+offset)
            {
                int y = ip.StackStack.Pop(), x = ip.StackStack.Pop();
                ip.StackStack.Push(_space[new FungeVector(x + ip.Offset.X, y + ip.Offset.Y)]);
                break;
            }

            case 'p': // Put: write cell at (x+offset, y+offset)
            {
                int y = ip.StackStack.Pop(), x = ip.StackStack.Pop();
                var val = ip.StackStack.Pop();
                _space[new FungeVector(x + ip.Offset.X, y + ip.Offset.Y)] = val;
                break;
            }

            // ── I/O ──────────────────────────────────────────────────────────
            case '.': // Output Integer
                _output.Write(ip.StackStack.Pop());
                _output.Write(' ');
                break;

            case ',': // Output Character
                _output.Write((char)ip.StackStack.Pop());
                break;

            case '&': // Input Integer
            {
                var line = _input.ReadLine();
                if (line is null) { ip.Delta = ip.Delta.Reflect(); break; }
                ip.StackStack.Push(int.TryParse(line.Trim(), out var v) ? v : 0);
                break;
            }

            case '~': // Input Character
            {
                var ch = _input.Read();
                if (ch < 0) ip.Delta = ip.Delta.Reflect();
                else ip.StackStack.Push(ch);
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

                // Advance to find next non-space instruction
                var instrPos = _space.Advance(ip.Position, ip.Delta);
                while (_space[instrPos] == ' ')
                    instrPos = _space.Advance(instrPos, ip.Delta);

                if (n == 0)
                {
                    // Skip the instruction; IP ends at instrPos, normal advance moves past
                    ip.Position = instrPos;
                }
                else
                {
                    // Execute n times; reset position to instrPos before each execution
                    for (var i = 0; i < n && !ip.IsStopped && !quit; i++)
                    {
                        ip.Position = instrPos;
                        var dummy = false;
                        ExecuteInstruction(ip, ips, ipNode, ref exitCode, ref quit, ref dummy);
                    }
                    ip.Position = instrPos;
                }
                // suppressAdvance = false: normal advance moves IP past instrPos
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

                // Restore storage offset (Y on top, then X)
                var oy = ip.StackStack.Pop();
                var ox = ip.StackStack.Pop();
                ip.Offset = new FungeVector(ox, oy);

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

            // ── Optional / 3-D-only (reflect) ────────────────────────────────
            case '=': // Execute (system exec) – reflect
            {
                // Consume 0gnirts command string from stack
                while (ip.StackStack.Pop() != 0) { }
                ip.Delta = ip.Delta.Reflect();
                break;
            }

            case 'i': // Input File – reflect
            case 'o': // Output File – reflect
            case 'h': // Go High (3-D) – reflect
            case 'l': // Go Low (3-D) – reflect
            case 'm': // High-Low If (3-D) – reflect
                ip.Delta = ip.Delta.Reflect();
                break;

            default:
                // A-Z: fingerprint-defined; reflect if not loaded
                if (cell is >= 'A' and <= 'Z')
                    ip.Delta = ip.Delta.Reflect();
                // All other characters: no-op
                break;
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

        // 1. Flags: bit 0 = /t (concurrency supported)
        items.Add(1);
        // 2. Cell size in bytes
        items.Add(4);
        // 3. Interpreter handprint ("Fung" as big-endian int)
        items.Add(unchecked((int)0x46756E67u));
        // 4. Version (Funge-98 = 9800)
        items.Add(9800);
        // 5. Operating paradigm (0 = system() unavailable)
        items.Add(0);
        // 6. Path separator
        items.Add(Path.DirectorySeparatorChar);
        // 7. Number of dimensions (2 = Befunge)
        items.Add(2);
        // 8. IP unique ID
        items.Add(ip.Id);
        // 9. IP team number
        items.Add(0);
        // 10-11. IP position (X, Y; Y on top)
        items.Add(ip.Position.X);
        items.Add(ip.Position.Y);
        // 12-13. IP delta (dX, dY; dY on top)
        items.Add(ip.Delta.X);
        items.Add(ip.Delta.Y);
        // 14-15. Storage offset (oX, oY; oY on top)
        items.Add(ip.Offset.X);
        items.Add(ip.Offset.Y);
        // 16-17. Least point of LSAB (minX, minY; minY on top)
        items.Add(_space.MinX);
        items.Add(_space.MinY);
        // 18-19. Greatest point of LSAB (maxX, maxY; maxY on top)
        items.Add(_space.MaxX);
        items.Add(_space.MaxY);

        var now = DateTime.Now;
        // 20. Current date: (year-1900)*10000 + month*100 + day
        items.Add(((now.Year - 1900) * 10000) + (now.Month * 100) + now.Day);
        // 21. Current time: HH*10000 + MM*100 + SS
        items.Add((now.Hour * 10000) + (now.Minute * 100) + now.Second);
        // 22. Number of stacks in stack stack
        items.Add(ip.StackStack.StackCount);
        // 23+. Size of each stack (TOSS first)
        foreach (var stack in ip.StackStack.AllStacks)
            items.Add(stack.Count);
        // Command-line args: empty list (single 0 terminator)
        items.Add(0);
        // Environment variables: empty list (single 0 terminator)
        items.Add(0);

        // Push in reverse order so items[0] ends up on top (= item 1)
        for (var i = items.Count - 1; i >= 0; i--)
            ip.StackStack.Push(items[i]);

        if (c > 0)
        {
            // Pick the c-th item from top of pushed items
            var popped = new int[items.Count];
            for (var i = 0; i < items.Count; i++)
                popped[i] = ip.StackStack.Pop();
            ip.StackStack.Push(c <= items.Count ? popped[c - 1] : 0);
        }
    }
}
