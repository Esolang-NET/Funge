using System.Text;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Fingerprints.File;

sealed class TestContext : IFungeExecutionContext, IFungeInstructionPointerContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];

    public bool Reflected { get; set; }
    public int InstructionPointerId { get; set; }
    public (int X, int Y, int Z) StorageOffset { get; set; }
    public int Dimensions => 3;

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public void PushString(string value)
    {
        Push(0);
        for (var i = 0; i < value.Length; i++)
            Push(value[i]);
    }

    public string PopString()
    {
        var sb = new StringBuilder();
        int c;
        while ((c = Pop()) != 0)
            sb.Insert(0, (char)c);
        return sb.ToString();
    }

    public (int X, int Y, int Z) PopVector()
    {
        var z = Pop();
        var y = Pop();
        var x = Pop();
        return (x, y, z);
    }

    public void PushVector(int x, int y, int z)
    {
        Push(x);
        Push(y);
        Push(z);
    }

    public int GetCell(int x, int y, int z) => _cells.TryGetValue((x, y, z), out var value) ? value : ' ';

    public void SetCell(int x, int y, int z, int value) => _cells[(x, y, z)] = value;
}

sealed class CoreOnlyContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();
    public bool Reflected { get; set; }
    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    public void PushString(string value)
    {
        Push(0);
        for (var i = 0; i < value.Length; i++)
            Push(value[i]);
    }
}

public class FileFingerprintTests
{
    static async Task<FingerprintInstruction> Instruction(FileFingerprint fp, char ch)
    {
        await Assert.That(fp.Instructions.TryGetValue(ch, out var instr)).IsTrue();
        Assert.NotNull(instr);
        return instr;
    }

    [Test]
    public async Task Handprint_IsFileMagic()
        => await Assert.That(new FileFingerprint().Handprint).IsEqualTo(0x46494C45);

    [Test]
    public async Task Open_Close_And_Tell_Work()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext { StorageOffset = (10, 20, 30) };
            ctx.PushVector(2, 0, 0);
            ctx.Push(1);
            ctx.PushString(path);

            await (await Instruction(fp, 'O'))(ctx);

            await Assert.That(ctx.Reflected).IsFalse();
            var handle = ctx.Pop();
            await Assert.That(handle).IsGreaterThan(0);

            ctx.Push(handle);
            await (await Instruction(fp, 'L'))(ctx);
            await Assert.That(ctx.Pop()).IsEqualTo(0);
            await Assert.That(ctx.Pop()).IsEqualTo(handle);

            ctx.Push(handle);
            await (await Instruction(fp, 'C'))(ctx);
            await Assert.That(ctx.Reflected).IsFalse();
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [Test]
    public async Task Put_And_Get_String_FollowSpecStackContract()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext();
            ctx.PushVector(0, 0, 0);
            ctx.Push(4);
            ctx.PushString(path);
            await (await Instruction(fp, 'O'))(ctx);
            var handle = ctx.Pop();

            ctx.Push(handle);
            ctx.PushString("foo\nbar\nbaz");
            await (await Instruction(fp, 'P'))(ctx);
            await Assert.That(ctx.Pop()).IsEqualTo(handle);

            ctx.Push(handle);
            ctx.Push(0);
            ctx.Push(0);
            await (await Instruction(fp, 'S'))(ctx);
            await Assert.That(ctx.Pop()).IsEqualTo(handle);

            ctx.Push(handle);
            await (await Instruction(fp, 'G'))(ctx);
            var length = ctx.Pop();
            await Assert.That(ctx.PopString()).IsEqualTo("foo\n");
            await Assert.That(length).IsEqualTo(4);
            await Assert.That(ctx.Pop()).IsEqualTo(handle);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [Test]
    public async Task Read_And_Write_Bytes_UseConfiguredBuffer()
    {
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllBytes(path, Encoding.ASCII.GetBytes("bar\nbaz"));
            using var fp = new FileFingerprint();
            var readCtx = new TestContext { StorageOffset = (10, 20, 30) };
            readCtx.PushVector(2, 0, 0);
            readCtx.Push(0);
            readCtx.PushString(path);
            await (await Instruction(fp, 'O'))(readCtx);
            var readHandle = readCtx.Pop();

            readCtx.Push(readHandle);
            readCtx.Push(7);
            await (await Instruction(fp, 'R'))(readCtx);
            await Assert.That(readCtx.Pop()).IsEqualTo(readHandle);
            await Assert.That(readCtx.GetCell(12, 20, 30)).IsEqualTo('b');
            await Assert.That(readCtx.GetCell(13, 20, 30)).IsEqualTo('a');
            await Assert.That(readCtx.GetCell(18, 20, 30)).IsEqualTo('z');
            readCtx.Push(readHandle);
            await (await Instruction(fp, 'C'))(readCtx);

            var writePath = path + ".copy";
            var writeCtx = new TestContext { StorageOffset = (10, 20, 30) };
            for (var i = 0; i < 7; i++)
                writeCtx.SetCell(12 + i, 20, 30, readCtx.GetCell(12 + i, 20, 30));
            writeCtx.PushVector(2, 0, 0);
            writeCtx.Push(4);
            writeCtx.PushString(writePath);
            await (await Instruction(fp, 'O'))(writeCtx);
            var writeHandle = writeCtx.Pop();

            writeCtx.Push(writeHandle);
            writeCtx.Push(7);
            await (await Instruction(fp, 'W'))(writeCtx);
            await Assert.That(writeCtx.Pop()).IsEqualTo(writeHandle);
            writeCtx.Push(writeHandle);
            await (await Instruction(fp, 'C'))(writeCtx);
            await Assert.That(System.IO.File.ReadAllText(writePath)).IsEqualTo("bar\nbaz");
            System.IO.File.Delete(writePath);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [Test]
    public async Task Seek_MovesFilePointer()
    {
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(path, "abcdef");
            using var fp = new FileFingerprint();
            var ctx = new TestContext();
            ctx.PushVector(0, 0, 0);
            ctx.Push(3);
            ctx.PushString(path);
            await (await Instruction(fp, 'O'))(ctx);
            var handle = ctx.Pop();

            ctx.Push(handle);
            ctx.Push(0);
            ctx.Push(2);
            await (await Instruction(fp, 'S'))(ctx);
            await Assert.That(ctx.Pop()).IsEqualTo(handle);

            ctx.Push(handle);
            await (await Instruction(fp, 'L'))(ctx);
            await Assert.That(ctx.Pop()).IsEqualTo(2);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [Test]
    public async Task Delete_RemovesExistingFile()
    {
        var path = Path.GetTempFileName();
        using var fp = new FileFingerprint();
        var ctx = new TestContext();
        ctx.PushString(path);

        await (await Instruction(fp, 'D'))(ctx);

        await Assert.That(ctx.Reflected).IsFalse();
        await Assert.That(System.IO.File.Exists(path)).IsFalse();
    }

    [Test]
    public async Task MissingBufferCapability_Reflects()
    {
        using var fp = new FileFingerprint();
        var ctx = new CoreOnlyContext();
        ctx.Push(1);
        ctx.PushString("file.tmp");

        await (await Instruction(fp, 'O'))(ctx);

        await Assert.That(ctx.Reflected).IsTrue();
    }

    [Test]
    public async Task Handles_AreScopedPerInstructionPointer_AndCopiedOnClone()
    {
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(path, "abcdef");
            using var fp = new FileFingerprint();
            var parent = new TestContext { InstructionPointerId = 1 };
            parent.PushVector(0, 0, 0);
            parent.Push(3);
            parent.PushString(path);
            await (await Instruction(fp, 'O'))(parent);
            var handle = parent.Pop();

            parent.Push(handle);
            parent.Push(0);
            parent.Push(2);
            await (await Instruction(fp, 'S'))(parent);
            await Assert.That(parent.Pop()).IsEqualTo(handle);

            fp.OnInstructionPointerCloned(1, 2);
            var child = new TestContext { InstructionPointerId = 2 };
            child.Push(handle);
            await (await Instruction(fp, 'L'))(child);
            await Assert.That(child.Pop()).IsEqualTo(2);
            await Assert.That(child.Pop()).IsEqualTo(handle);

            child.Push(handle);
            parent.Push(handle);
            parent.Push(0);
            parent.Push(1);
            await (await Instruction(fp, 'S'))(parent);
            await Assert.That(parent.Pop()).IsEqualTo(handle);

            child.Push(handle);
            await (await Instruction(fp, 'L'))(child);
            await Assert.That(child.Pop()).IsEqualTo(2);
            await Assert.That(child.Pop()).IsEqualTo(handle);

            fp.OnInstructionPointerTerminated(1);
            child.Push(handle);
            await (await Instruction(fp, 'L'))(child);
            await Assert.That(child.Pop()).IsEqualTo(2);
            await Assert.That(child.Pop()).IsEqualTo(handle);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }
}
