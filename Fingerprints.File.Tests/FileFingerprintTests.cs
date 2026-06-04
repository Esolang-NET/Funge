namespace Esolang.Funge.Fingerprints.File.Tests;

/// <summary>Simple in-memory execution context for testing fingerprint instructions.</summary>
sealed class TestContext : IFungeExecutionContext
{
    readonly Stack<int> _stack = new();

    public bool Reflected { get; set; }

    public void Push(int value) => _stack.Push(value);
    public int Pop() => _stack.Count > 0 ? _stack.Pop() : 0;
    public int Peek() => _stack.Count > 0 ? _stack.Peek() : 0;
    public void Reflect() => Reflected = true;

    /// <summary>Push a null-terminated string (0gnirts) onto the stack so Pop0gnirts can read it.</summary>
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

    // -----------------------------------------------------------------------
    // Handprint
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Handprint_IsFileMagic()
        => Assert.AreEqual(0x46494C45, new FileFingerprint().Handprint);

    // -----------------------------------------------------------------------
    // O / C — Open and Close
    // -----------------------------------------------------------------------

    [TestMethod]
    public void OpenClose_ReadMode_PushesAndReleasesHandle()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext();

            // O: push flags=0 (read), push filename 0gnirts
            ctx.PushString(path);
            ctx.Push(0); // flags: read
            Instruction(fp, 'O')(ctx);

            Assert.IsFalse(ctx.Reflected);
            var handle = ctx.Pop();
            Assert.IsGreaterThan(0, handle);

            // C: push handle
            ctx.Push(handle);
            Instruction(fp, 'C')(ctx);
            Assert.IsFalse(ctx.Reflected);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void Open_NonExistentFile_Reflects()
    {
        using var fp = new FileFingerprint();
        var ctx = new TestContext();
        ctx.PushString("__this_file_does_not_exist__.b98");
        ctx.Push(0); // flags: read
        Instruction(fp, 'O')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    // -----------------------------------------------------------------------
    // W / R — Write and Read byte
    // -----------------------------------------------------------------------

    [TestMethod]
    public void WriteRead_Byte_RoundTrips()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();

            // Open for write
            var wCtx = new TestContext();
            wCtx.PushString(path);
            wCtx.Push(1); // flags: write
            Instruction(fp, 'O')(wCtx);
            var wHandle = wCtx.Pop();

            // W: push byte first, then handle (handle is popped first)
            wCtx.Push(0x41); // 'A'
            wCtx.Push(wHandle);
            Instruction(fp, 'W')(wCtx);
            Assert.IsFalse(wCtx.Reflected);

            // Close
            wCtx.Push(wHandle);
            Instruction(fp, 'C')(wCtx);

            // Open for read
            var rCtx = new TestContext();
            rCtx.PushString(path);
            rCtx.Push(0); // flags: read
            Instruction(fp, 'O')(rCtx);
            var rHandle = rCtx.Pop();

            // R: push handle
            rCtx.Push(rHandle);
            Instruction(fp, 'R')(rCtx);
            Assert.IsFalse(rCtx.Reflected);
            Assert.AreEqual(0x41, rCtx.Pop());

            // R again → EOF → reflect
            rCtx.Push(rHandle);
            Instruction(fp, 'R')(rCtx);
            Assert.IsTrue(rCtx.Reflected);
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    // -----------------------------------------------------------------------
    // P / G — Put and Get string
    // -----------------------------------------------------------------------

    [TestMethod]
    public void PutGet_String_RoundTrips()
    {
        var path = Path.GetTempFileName();
        try
        {
            using var fp = new FileFingerprint();

            // Open for write and put "Hello"
            var wCtx = new TestContext();
            wCtx.PushString(path);
            wCtx.Push(1);
            Instruction(fp, 'O')(wCtx);
            var wHandle = wCtx.Pop();

            // P: push 0gnirts first, then handle (handle is popped first)
            wCtx.PushString("Hello");
            wCtx.Push(wHandle);
            Instruction(fp, 'P')(wCtx);
            Assert.IsFalse(wCtx.Reflected);
            wCtx.Push(wHandle);
            Instruction(fp, 'C')(wCtx);

            // Open for read and get string
            var rCtx = new TestContext();
            rCtx.PushString(path);
            rCtx.Push(0);
            Instruction(fp, 'O')(rCtx);
            var rHandle = rCtx.Pop();

            rCtx.Push(rHandle);
            Instruction(fp, 'G')(rCtx);
            Assert.IsFalse(rCtx.Reflected);

            // Read back the 0gnirts
            var sb = new System.Text.StringBuilder();
            int c;
            while ((c = rCtx.Pop()) != 0)
                sb.Insert(0, (char)c);
            Assert.AreEqual("Hello", sb.ToString());
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    // -----------------------------------------------------------------------
    // S — File size
    // -----------------------------------------------------------------------

    [TestMethod]
    public void FileSize_ReturnsCorrectSize()
    {
        var path = Path.GetTempFileName();
        try
        {
            System.IO.File.WriteAllText(path, "ABCDE");
            using var fp = new FileFingerprint();
            var ctx = new TestContext();
            ctx.PushString(path);
            Instruction(fp, 'S')(ctx);
            Assert.IsFalse(ctx.Reflected);
            Assert.AreEqual(5, ctx.Pop());
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }

    [TestMethod]
    public void FileSize_NonExistentFile_Reflects()
    {
        using var fp = new FileFingerprint();
        var ctx = new TestContext();
        ctx.PushString("__no_such_file__");
        Instruction(fp, 'S')(ctx);
        Assert.IsTrue(ctx.Reflected);
    }

    // -----------------------------------------------------------------------
    // D — Delete
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Delete_ExistingFile_RemovesIt()
    {
        var path = Path.GetTempFileName();
        using var fp = new FileFingerprint();
        var ctx = new TestContext();
        ctx.PushString(path);
        Instruction(fp, 'D')(ctx);
        Assert.IsFalse(ctx.Reflected);
        Assert.IsFalse(System.IO.File.Exists(path));
    }

    // -----------------------------------------------------------------------
    // M — Move / Rename
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Move_ExistingFile_RenamesIt()
    {
        var src = Path.GetTempFileName();
        var dest = src + ".moved";
        try
        {
            using var fp = new FileFingerprint();
            var ctx = new TestContext();
            // M: pop dest, pop src
            ctx.PushString(src);
            ctx.PushString(dest);
            Instruction(fp, 'M')(ctx);
            Assert.IsFalse(ctx.Reflected);
            Assert.IsFalse(System.IO.File.Exists(src));
            Assert.IsTrue(System.IO.File.Exists(dest));
        }
        finally
        {
            if (System.IO.File.Exists(dest)) System.IO.File.Delete(dest);
        }
    }

    // -----------------------------------------------------------------------
    // Dispose
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Dispose_ClosesAllOpenHandles()
    {
        var path = Path.GetTempFileName();
        try
        {
            var fp = new FileFingerprint();
            var ctx = new TestContext();
            ctx.PushString(path);
            ctx.Push(1);
            Instruction(fp, 'O')(ctx);
            _ = ctx.Pop(); // discard handle

            fp.Dispose();

            // File should be writable again after dispose
            System.IO.File.WriteAllText(path, "ok");
        }
        finally
        {
            System.IO.File.Delete(path);
        }
    }
}
