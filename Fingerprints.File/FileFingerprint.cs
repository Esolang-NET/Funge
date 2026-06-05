namespace Esolang.Funge.Fingerprints.File;

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
///   <item><term><c>G</c></term><description>Get string. Peek handle. Push 0gnirts and byte count. Reflect on error.</description></item>
///   <item><term><c>L</c></term><description>Get location. Peek handle. Push current file position. Reflect on error.</description></item>
///   <item><term><c>O</c></term><description>Open file. Pop 0gnirts filename, flags, and buffer vector. Push handle. Reflect on error.</description></item>
///   <item><term><c>P</c></term><description>Put string. Pop 0gnirts, peek handle, and write to file. Reflect on error.</description></item>
///   <item><term><c>R</c></term><description>Read bytes. Pop count, peek handle, and copy bytes into the configured buffer vector. Reflect on short read or error.</description></item>
///   <item><term><c>S</c></term><description>Seek. Pop offset and mode, peek handle, and reposition the stream. Reflect on error.</description></item>
///   <item><term><c>W</c></term><description>Write bytes. Pop count, peek handle, and write bytes from the configured buffer vector. Reflect on error.</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class FileFingerprint : IFingerprint, IDisposable
{
    sealed class FileHandle(Stream stream, int bufferX, int bufferY, int bufferZ, bool appendOnWrite)
    {
        public Stream Stream { get; } = stream;
        public int BufferX { get; } = bufferX;
        public int BufferY { get; } = bufferY;
        public int BufferZ { get; } = bufferZ;
        public bool AppendOnWrite { get; } = appendOnWrite;
    }

    int _nextHandle = 1;
    readonly Dictionary<int, FileHandle> _handles = [];
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
            .Add('L', GetLocation)
            .Add('O', OpenFile)
            .Add('P', PutString)
            .Add('R', ReadBytes)
            .Add('S', Seek)
            .Add('W', WriteBytes)
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

    static bool TryGetBufferOrigin(
        IFungeExecutionContext ctx,
        out int x,
        out int y,
        out int z)
    {
        if (ctx is not IFungeVectorContext vector || ctx is not IFungeStorageOffsetContext offset)
        {
            x = y = z = 0;
            ctx.Reflect();
            return false;
        }

        var (vx, vy, vz) = vector.PopVector();
        var (ox, oy, oz) = offset.StorageOffset;
        x = vx + ox;
        y = vy + oy;
        z = vz + oz;
        return true;
    }

    static bool TryGetSpaceContext(IFungeExecutionContext ctx, out IFungeSpaceContext space)
    {
        if (ctx is not IFungeSpaceContext foundSpace)
        {
            space = null!;
            ctx.Reflect();
            return false;
        }

        space = foundSpace;
        return true;
    }

    static byte[] StringToBytes(string value)
        => [.. value.Select(static ch => (byte)(ch & 0xFF))];

    static string BytesToString(byte[] bytes, int count)
        => new([.. bytes.Take(count).Select(static b => (char)b)]);

    static bool IsRecoverableFileException(Exception exception)
        => exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or ObjectDisposedException
            or ArgumentException;

    static bool TryReadLine(Stream stream, out byte[] bytes)
    {
        bytes = [];
        using var buffer = new MemoryStream();
        try
        {
            while (true)
            {
                var value = stream.ReadByte();
                switch (value)
                {
                    case -1:
                        bytes = buffer.ToArray();
                        return true;
                    case '\r':
                        buffer.WriteByte((byte)value);
                        var next = stream.ReadByte();
                        if (next == '\n')
                            buffer.WriteByte((byte)next);
                        else if (next >= 0 && stream.CanSeek)
                            stream.Position--;

                        bytes = buffer.ToArray();
                        return true;
                    case '\n':
                        buffer.WriteByte((byte)value);
                        bytes = buffer.ToArray();
                        return true;
                    default:
                        buffer.WriteByte((byte)value);
                        break;
                }
            }
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            return false;
        }
    }

    static void SeekToWritePosition(FileHandle handle)
    {
        if (handle.AppendOnWrite && handle.Stream.CanSeek)
            handle.Stream.Seek(0, SeekOrigin.End);
    }

    void OpenFile(IFungeExecutionContext ctx)
    {
        var filename = Pop0gnirts(ctx);
        var flags = ctx.Pop();
        if (!TryGetBufferOrigin(ctx, out var bufferX, out var bufferY, out var bufferZ))
            return;

        try
        {
            Stream stream;
            var appendOnWrite = false;
            switch (flags)
            {
                case 0:
                    stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    break;
                case 1:
                    stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
                    break;
                case 2:
                    stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                    stream.Seek(0, SeekOrigin.Begin);
                    appendOnWrite = true;
                    break;
                case 3:
                    stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    break;
                case 4:
                    stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
                    break;
                case 5:
                    stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                    stream.Seek(0, SeekOrigin.Begin);
                    appendOnWrite = true;
                    break;
                default:
                    ctx.Reflect();
                    return;
            }

            var handle = _nextHandle++;
            _handles[handle] = new FileHandle(stream, bufferX, bufferY, bufferZ, appendOnWrite);
            ctx.Push(handle);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    void CloseFile(IFungeExecutionContext ctx)
    {
        var handle = ctx.Pop();
        if (!_handles.TryGetValue(handle, out var fileHandle))
        {
            ctx.Reflect();
            return;
        }

        try
        {
            fileHandle.Stream.Dispose();
            _handles.Remove(handle);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    void GetString(IFungeExecutionContext ctx)
    {
        var handle = ctx.Peek();
        if (!_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanRead)
        {
            ctx.Reflect();
            return;
        }

        if (!TryReadLine(fileHandle.Stream, out var bytes))
        {
            ctx.Reflect();
            return;
        }

        Push0gnirts(ctx, BytesToString(bytes, bytes.Length));
        ctx.Push(bytes.Length);
    }

    void GetLocation(IFungeExecutionContext ctx)
    {
        var handle = ctx.Peek();
        if (!_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanSeek)
        {
            ctx.Reflect();
            return;
        }

        try
        {
            ctx.Push(checked((int)fileHandle.Stream.Position));
        }
        catch (Exception ex) when (IsRecoverableFileException(ex) || ex is OverflowException)
        {
            ctx.Reflect();
        }
    }

    void PutString(IFungeExecutionContext ctx)
    {
        var value = Pop0gnirts(ctx);
        var handle = ctx.Peek();
        if (!_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanWrite)
        {
            ctx.Reflect();
            return;
        }

        try
        {
            SeekToWritePosition(fileHandle);
            var bytes = StringToBytes(value);
            fileHandle.Stream.Write(bytes, 0, bytes.Length);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    static void DeleteFile(IFungeExecutionContext ctx)
    {
        var filename = Pop0gnirts(ctx);
        try
        {
            if (!System.IO.File.Exists(filename))
            {
                ctx.Reflect();
                return;
            }

            System.IO.File.Delete(filename);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    void ReadBytes(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var handle = ctx.Peek();
        if (count <= 0 || !_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanRead)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetSpaceContext(ctx, out var space))
            return;

        try
        {
            var buffer = new byte[count];
            var bytesRead = fileHandle.Stream.Read(buffer, 0, count);
            for (var i = 0; i < bytesRead; i++)
                space.SetCell(fileHandle.BufferX + i, fileHandle.BufferY, fileHandle.BufferZ, buffer[i]);
            if (bytesRead != count)
                ctx.Reflect();
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    void Seek(IFungeExecutionContext ctx)
    {
        var offset = ctx.Pop();
        var mode = ctx.Pop();
        var handle = ctx.Peek();
        if (!_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanSeek)
        {
            ctx.Reflect();
            return;
        }

        try
        {
            var origin = mode switch
            {
                0 => SeekOrigin.Begin,
                1 => SeekOrigin.Current,
                2 => SeekOrigin.End,
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
            fileHandle.Stream.Seek(offset, origin);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex) || ex is ArgumentOutOfRangeException)
        {
            ctx.Reflect();
        }
    }

    void WriteBytes(IFungeExecutionContext ctx)
    {
        var count = ctx.Pop();
        var handle = ctx.Peek();
        if (count <= 0 || !_handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanWrite)
        {
            ctx.Reflect();
            return;
        }

        if (!TryGetSpaceContext(ctx, out var space))
            return;

        try
        {
            SeekToWritePosition(fileHandle);
            var buffer = new byte[count];
            for (var i = 0; i < count; i++)
                buffer[i] = (byte)(space.GetCell(fileHandle.BufferX + i, fileHandle.BufferY, fileHandle.BufferZ) & 0xFF);
            fileHandle.Stream.Write(buffer, 0, buffer.Length);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var handle in _handles.Values)
            handle.Stream.Dispose();
        _handles.Clear();
    }
}

