using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.File;

/// <summary>
/// Provides the standard Funge-98 <c>FILE</c> fingerprint (handprint <c>0x46494C45</c>).
/// </summary>
/// <remarks>
/// <para>
/// The FILE fingerprint provides file I/O operations.
/// File handles are managed per instruction pointer within the fingerprint instance;
/// dispose the instance to release any remaining open handles.
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
public sealed class FileFingerprint : IFingerprint, IFungeInstructionPointerLifecycle, IDisposable
{
    sealed class FileHandle(
        string path,
        Stream stream,
        int flags,
        int bufferX,
        int bufferY,
        int bufferZ,
        bool appendOnWrite)
    {
        public string Path { get; } = path;
        public Stream Stream { get; } = stream;
        public int Flags { get; } = flags;
        public int BufferX { get; } = bufferX;
        public int BufferY { get; } = bufferY;
        public int BufferZ { get; } = bufferZ;
        public bool AppendOnWrite { get; } = appendOnWrite;
    }

    sealed class InstructionPointerFileState
    {
        public int NextHandle { get; set; } = 1;
        public Dictionary<int, FileHandle> Handles { get; } = [];
    }

    readonly Dictionary<int, InstructionPointerFileState> _statesByInstructionPointer = [];
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

    static Stream CreateStream(string path, int flags, out bool appendOnWrite)
    {
        appendOnWrite = false;
        return flags switch
        {
            0 => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
            1 => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite),
            2 => CreateAppendWriteStream(path, out appendOnWrite),
            3 => new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite),
            4 => new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite),
            5 => CreateAppendReadWriteStream(path, out appendOnWrite),
            _ => throw new ArgumentOutOfRangeException(nameof(flags)),
        };

        static Stream CreateAppendWriteStream(string path, out bool appendOnWrite)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.Begin);
            appendOnWrite = true;
            return stream;
        }

        static Stream CreateAppendReadWriteStream(string path, out bool appendOnWrite)
        {
            var stream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            stream.Seek(0, SeekOrigin.Begin);
            appendOnWrite = true;
            return stream;
        }
    }

    static bool TryGetInstructionPointerId(IFungeExecutionContext ctx, out int instructionPointerId)
    {
        if (ctx is not IFungeInstructionPointerContext instructionPointer)
        {
            instructionPointerId = 0;
            ctx.Reflect();
            return false;
        }

        instructionPointerId = instructionPointer.InstructionPointerId;
        return true;
    }

    bool TryGetInstructionPointerState(IFungeExecutionContext ctx, out InstructionPointerFileState state)
    {
        if (!TryGetInstructionPointerId(ctx, out var instructionPointerId))
        {
            state = null!;
            return false;
        }

        if (!_statesByInstructionPointer.TryGetValue(instructionPointerId, out var existingState))
        {
            state = new InstructionPointerFileState();
            _statesByInstructionPointer[instructionPointerId] = state;
        }
        else
        {
            state = existingState;
        }

        return true;
    }

    void OpenFile(IFungeExecutionContext ctx)
    {
        var filename = Pop0gnirts(ctx);
        var flags = ctx.Pop();
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;
        if (!TryGetBufferOrigin(ctx, out var bufferX, out var bufferY, out var bufferZ))
            return;

        try
        {
            var stream = CreateStream(filename, flags, out var appendOnWrite);
            var handle = state.NextHandle++;
            state.Handles[handle] = new FileHandle(filename, stream, flags, bufferX, bufferY, bufferZ, appendOnWrite);
            ctx.Push(handle);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex) || ex is ArgumentOutOfRangeException)
        {
            ctx.Reflect();
        }
    }

    void CloseFile(IFungeExecutionContext ctx)
    {
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var handle = ctx.Pop();
        if (!state.Handles.TryGetValue(handle, out var fileHandle))
        {
            ctx.Reflect();
            return;
        }

        try
        {
            fileHandle.Stream.Dispose();
            state.Handles.Remove(handle);
        }
        catch (Exception ex) when (IsRecoverableFileException(ex))
        {
            ctx.Reflect();
        }
    }

    void GetString(IFungeExecutionContext ctx)
    {
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var handle = ctx.Peek();
        if (!state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanRead)
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
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var handle = ctx.Peek();
        if (!state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanSeek)
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
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var value = Pop0gnirts(ctx);
        var handle = ctx.Peek();
        if (!state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanWrite)
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
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var count = ctx.Pop();
        var handle = ctx.Peek();
        if (count <= 0 || !state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanRead)
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
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var offset = ctx.Pop();
        var mode = ctx.Pop();
        var handle = ctx.Peek();
        if (!state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanSeek)
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
        if (!TryGetInstructionPointerState(ctx, out var state))
            return;

        var count = ctx.Pop();
        var handle = ctx.Peek();
        if (count <= 0 || !state.Handles.TryGetValue(handle, out var fileHandle) || !fileHandle.Stream.CanWrite)
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
    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
    {
        if (!_statesByInstructionPointer.TryGetValue(parentInstructionPointerId, out var parentState))
            return;

        var childState = new InstructionPointerFileState
        {
            NextHandle = parentState.NextHandle,
        };

        try
        {
            foreach (var (handle, fileHandle) in parentState.Handles)
            {
                var stream = CreateStream(fileHandle.Path, fileHandle.Flags, out _);
                if (stream.CanSeek && fileHandle.Stream.CanSeek)
                    stream.Seek(fileHandle.Stream.Position, SeekOrigin.Begin);
                childState.Handles[handle] = new FileHandle(
                    fileHandle.Path,
                    stream,
                    fileHandle.Flags,
                    fileHandle.BufferX,
                    fileHandle.BufferY,
                    fileHandle.BufferZ,
                    fileHandle.AppendOnWrite);
            }
        }
        catch
        {
            foreach (var handle in childState.Handles.Values)
                handle.Stream.Dispose();
            throw;
        }

        _statesByInstructionPointer[childInstructionPointerId] = childState;
    }

    /// <inheritdoc/>
    public void OnInstructionPointerTerminated(int instructionPointerId)
    {
        if (!_statesByInstructionPointer.TryGetValue(instructionPointerId, out var state))
            return;

        foreach (var handle in state.Handles.Values)
            handle.Stream.Dispose();
        _statesByInstructionPointer.Remove(instructionPointerId);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var state in _statesByInstructionPointer.Values)
            foreach (var handle in state.Handles.Values)
                handle.Stream.Dispose();
        _statesByInstructionPointer.Clear();
    }
}
#if NETSTANDARD2_0
static class KeyValuePairExtensions
{
    public static void Deconstruct<K, V>(this KeyValuePair<K, V> kvp, out K key, out V value)
       => (key, value) = (kvp.Key, kvp.Value);
}
#endif
