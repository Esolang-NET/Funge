using Esolang.Funge;
using System.Text;

// string return (file-based)
Console.WriteLine($"{nameof(FungeSample.HelloWorld)}: {FungeSample.HelloWorld()}");

// Task<string> return (file-based)
Console.WriteLine($"{nameof(FungeSample.HelloWorldAsync)}: {await FungeSample.HelloWorldAsync()}");

// void with TextWriter output (file-based)
using var textWriter = new StringWriter();
FungeSample.HelloWorldWriter(textWriter);
Console.WriteLine($"{nameof(FungeSample.HelloWorldWriter)}: {textWriter}");

// IEnumerable<byte> (file-based)
Console.WriteLine($"{nameof(FungeSample.HelloWorldBytes)}: {Encoding.UTF8.GetString(FungeSample.HelloWorldBytes().ToArray())}");

// IAsyncEnumerable<byte> (file-based)
Console.WriteLine($"{nameof(FungeSample.HelloWorldBytesAsync)}: {Encoding.UTF8.GetString(await ToByteArrayAsync(FungeSample.HelloWorldBytesAsync()))}");

// Inline source — same "Hello, World!" program embedded as a string literal
Console.WriteLine($"{nameof(FungeSample.HelloWorldInline)}: {FungeSample.HelloWorldInline()}");

static async Task<byte[]> ToByteArrayAsync(IAsyncEnumerable<byte> source)
{
    var list = new List<byte>();
    await foreach (var b in source)
        list.Add(b);
    return [.. list];
}

namespace Esolang.Funge
{
    partial class FungeSample
    {
        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial string HelloWorld();

        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial Task<string> HelloWorldAsync();

        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial void HelloWorldWriter(System.IO.TextWriter output);

        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial IEnumerable<byte> HelloWorldBytes();

        [GenerateFungeMethod("Programs/hello.b98")]
        public static partial IAsyncEnumerable<byte> HelloWorldBytesAsync();

        // InlineSource: no .b98 file needed — Funge-98 code is embedded directly
        [GenerateFungeMethod(InlineSource = "64+\"!dlroW ,olleH\">:#,_@")]
        public static partial string HelloWorldInline();
    }
}
