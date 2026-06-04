namespace Esolang.Funge;

/// <summary>
/// Provides the standard Funge-98 <c>FILE</c> fingerprint (handprint <c>0x46494C45</c>).
/// </summary>
/// <remarks>
/// <para>
/// The FILE fingerprint provides file I/O operations.
/// File handles are managed internally per instance; dispose the instance to release all open handles.
/// </para>
/// <para>
/// Supported instructions:
/// <list type="table">
///   <listheader><term>Instruction</term><description>Behaviour</description></listheader>
///   <item><term><c>C</c></term><description>Close file. Pop handle.</description></item>
///   <item><term><c>D</c></term><description>Delete file. Pop 0gnirts filename. Reflect on error.</description></item>
///   <item><term><c>G</c></term><description>Get string. Pop handle. Push 0gnirts (one line). Reflect on error.</description></item>
///   <item><term><c>M</c></term><description>Move (rename) file. Pop 0gnirts dest, pop 0gnirts src. Reflect on error.</description></item>
///   <item><term><c>O</c></term><description>Open file. Pop flags, pop 0gnirts filename. Push handle. Reflect on error. Flags: 0=read, 1=write (create/truncate), 2=append.</description></item>
///   <item><term><c>P</c></term><description>Put string. Pop handle, pop 0gnirts. Write to file. Reflect on error.</description></item>
///   <item><term><c>R</c></term><description>Read byte. Pop handle. Push byte. Reflect on EOF or error.</description></item>
///   <item><term><c>S</c></term><description>File size. Pop 0gnirts filename. Push size. Reflect on error.</description></item>
///   <item><term><c>W</c></term><description>Write byte. Pop handle, pop byte. Write to file. Reflect on error.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class FileFingerprint : IFingerprint, IDisposable
{
    int _nextHandle = 1;
    readonly Dictionary<int, Stream> _handles = [];
    bool _disposed;

    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("FILE");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="FileFingerprint"/>.</summary>
    public FileFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('C', CloseFile)
            .Add('D', DeleteFile)
            .Add('G', GetString)
            .Add('M', MoveFile)
            .Add('O', OpenFile)
            .Add('P', PutString)
            .Add('R', ReadByte)
            .Add('S', FileSize)
            .Add('W', WriteByte)
            .BuildInstructions();

    static string Pop0gnirts(IFungeExecutionContext ctx)
    {
        var sb = new System.Text.StringBuilder();
        int c;
        while ((c = ctx.Pop()) != 0)
            sb.Insert(0, (char)c);
        return sb.ToString();
    }

    static void Push0gnirts(IFungeExecutionContext ctx, string value)
    {
        ctx.Push(0);
        for (var i = 0; i < value.Length; i++)
            ctx.Push(value[i]);
    }

    void OpenFile(IFungeExecutionContext ctx)
    {
        var flags = ctx.Pop();
        var filename = Pop0gnirts(ctx);
        try
        {
            var stream = (flags & 3) switch
            {
                1 => new FileStream(filename, FileMode.Create, FileAccess.Write),
                2 => new FileStream(filename, FileMode.Append, FileAccess.Write),
                _ => new FileStream(filename, FileMode.Open, FileAccess.Read),
            };
            var handle = _nextHandle++;
            _handles[handle] = stream;
            ctx.Push(handle);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    void CloseFile(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        if (_handles.TryGetValue(handle, out var stream))
        {
            stream.Dispose();
            _handles.Remove(handle);
        }
    }

    void ReadByte(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        if (!_handles.TryGetValue(handle, out var stream))
        {
            ctx.Reflect();
            return;
        }
        try
        {
            var b = stream.ReadByte();
            if (b < 0)
                ctx.Reflect();
            else
                ctx.Push(b);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    void WriteByte(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        var b = ctx.Pop();
        if (!_handles.TryGetValue(handle, out var stream))
        {
            ctx.Reflect();
            return;
        }
        try
        {
            stream.WriteByte((byte)(b & 0xFF));
        }
        catch
        {
            ctx.Reflect();
        }
    }

    void GetString(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        if (!_handles.TryGetValue(handle, out var stream))
        {
            ctx.Reflect();
            return;
        }
        try
        {
            var sb = new System.Text.StringBuilder();
            int b;
            while ((b = stream.ReadByte()) >= 0 && b != '\n')
                sb.Append((char)b);
            if (sb.Length == 0 && b < 0)
            {
                ctx.Reflect();
                return;
            }
            // Strip trailing \r for Windows line endings
#if NETSTANDARD2_0
            if (sb.Length > 0 && sb[sb.Length - 1] == '\r')
#else
            if (sb.Length > 0 && sb[^1] == '\r')
#endif
                sb.Length--;
            Push0gnirts(ctx, sb.ToString());
        }
        catch
        {
            ctx.Reflect();
        }
    }

    void PutString(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        var value = Pop0gnirts(ctx);
        if (!_handles.TryGetValue(handle, out var stream))
        {
            ctx.Reflect();
            return;
        }
        try
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void DeleteFile(IFungeExecutionContext ctx)
    {
        var filename = Pop0gnirts(ctx);
        try
        {
            File.Delete(filename);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void MoveFile(IFungeExecutionContext ctx)
    {
        var dest = Pop0gnirts(ctx);
        var src = Pop0gnirts(ctx);
        try
        {
            File.Move(src, dest);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    static void FileSize(IFungeExecutionContext ctx)
    {
        var filename = Pop0gnirts(ctx);
        try
        {
            ctx.Push((int)new FileInfo(filename).Length);
        }
        catch
        {
            ctx.Reflect();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var stream in _handles.Values)
            stream.Dispose();
        _handles.Clear();
    }
}

