using System.Text;

namespace Esolang.Funge.Fingerprints.File;

sealed class TestContext : IFungeExecutionContext, IFungeVectorContext, IFungeSpaceContext, IFungeStorageOffsetContext
{
    readonly Stack<int> _stack = new();
    readonly Dictionary<(int X, int Y, int Z), int> _cells = [];

    public bool Reflected { get; set; }
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

[TestClass]
public class FileFingerprintTests
{
    static FingerprintInstruction Instruction(FileFingerprint fp, char ch)
    {
        Assert.IsTrue(fp.Instructions.TryGetValue(ch, out var instr));
        return instr;
    }

    [TestMethod]
    public void Handprint_IsFileMagic()
        => Assert.AreEqual(0x46494C45, new FileFingerprint().Handprint);

    [TestMethod]
    public void Open_Close_And_Tell_Work()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext { StorageOffset = (10, 20, 30) };
            ctx.PushVector(2, 0, 0);
            ctx.Push(1);
            ctx.PushString(path);

            Instruction(fp, 'O')(ctx);

            Assert.IsFalse(ctx.Reflected);
            var handle = ctx.Pop();
            Assert.IsGreaterThan(0, handle);

            ctx.Push(handle);
            Instruction(fp, 'L')(ctx);
            Assert.AreEqual(0, ctx.Pop());
            Assert.AreEqual(handle, ctx.Pop());

            ctx.Push(handle);
            Instruction(fp, 'C')(ctx);
            Assert.IsFalse(ctx.Reflected);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void Put_And_Get_String_FollowSpecStackContract()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext();
            ctx.PushVector(0, 0, 0);
            ctx.Push(4);
            ctx.PushString(path);
            Instruction(fp, 'O')(ctx);
            var handle = ctx.Pop();

            ctx.Push(handle);
            ctx.PushString("foo\nbar\nbaz");
            Instruction(fp, 'P')(ctx);
            Assert.AreEqual(handle, ctx.Pop());

            ctx.Push(handle);
            ctx.Push(0);
            ctx.Push(0);
            Instruction(fp, 'S')(ctx);
            Assert.AreEqual(handle, ctx.Pop());

            ctx.Push(handle);
            Instruction(fp, 'G')(ctx);
            var length = ctx.Pop();
            Assert.AreEqual("foo\n", ctx.PopString());
            Assert.AreEqual(4, length);
            Assert.AreEqual(handle, ctx.Pop());
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void Read_And_Write_Bytes_UseConfiguredBuffer()
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
            Instruction(fp, 'O')(readCtx);
            var readHandle = readCtx.Pop();

            readCtx.Push(readHandle);
            readCtx.Push(7);
            Instruction(fp, 'R')(readCtx);
            Assert.AreEqual(readHandle, readCtx.Pop());
            Assert.AreEqual('b', readCtx.GetCell(12, 20, 30));
            Assert.AreEqual('a', readCtx.GetCell(13, 20, 30));
            Assert.AreEqual('z', readCtx.GetCell(18, 20, 30));
            readCtx.Push(readHandle);
            Instruction(fp, 'C')(readCtx);

            var writePath = path + ".copy";
            var writeCtx = new TestContext { StorageOffset = (10, 20, 30) };
            for (var i = 0; i < 7; i++)
                writeCtx.SetCell(12 + i, 20, 30, readCtx.GetCell(12 + i, 20, 30));
            writeCtx.PushVector(2, 0, 0);
            writeCtx.Push(4);
            writeCtx.PushString(writePath);
            Instruction(fp, 'O')(writeCtx);
            var writeHandle = writeCtx.Pop();

            writeCtx.Push(writeHandle);
            writeCtx.Push(7);
            Instruction(fp, 'W')(writeCtx);
            Assert.AreEqual(writeHandle, writeCtx.Pop());
            writeCtx.Push(writeHandle);
            Instruction(fp, 'C')(writeCtx);
            Assert.AreEqual("bar\nbaz", System.IO.File.ReadAllText(writePath));
            System.IO.File.Delete(writePath);
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void Seek_MovesFilePointer()
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
            Instruction(fp, 'O')(ctx);
            var handle = ctx.Pop();

            ctx.Push(handle);
            ctx.Push(0);
            ctx.Push(2);
            Instruction(fp, 'S')(ctx);
            Assert.AreEqual(handle, ctx.Pop());

            ctx.Push(handle);
            Instruction(fp, 'L')(ctx);
            Assert.AreEqual(2, ctx.Pop());
        }
        finally
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void Delete_RemovesExistingFile()
    {
        var path = Path.GetTempFileName();
        using var fp = new FileFingerprint();
        var ctx = new TestContext();
        ctx.PushString(path);

        Instruction(fp, 'D')(ctx);

        Assert.IsFalse(ctx.Reflected);
        Assert.IsFalse(System.IO.File.Exists(path));
    }

    [TestMethod]
    public void MissingBufferCapability_Reflects()
    {
        using var fp = new FileFingerprint();
        var ctx = new CoreOnlyContext();
        ctx.Push(1);
        ctx.PushString("file.tmp");

        Instruction(fp, 'O')(ctx);

        Assert.IsTrue(ctx.Reflected);
    }
}
