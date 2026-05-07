using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Reflection;
using System.Text;

namespace Esolang.Funge.Generator.Tests;

[TestClass]
public class FungeMethodGeneratorTests
{
    public TestContext TestContext { get; set; } = default!;

    Compilation baseCompilation = default!;

    [TestInitialize]
    public void InitializeCompilation()
    {
        IEnumerable<PortableExecutableReference> references =
#if NET10_0_OR_GREATER
            Net100.References.All;
#elif NET9_0_OR_GREATER
            Net90.References.All;
#elif NET8_0_OR_GREATER
            Net80.References.All;
#elif NET472_OR_GREATER
            Net472.References.All;
#else
            throw new InvalidOperationException("Unsupported target framework for generator tests.");
#endif

        var referenceList = references.ToList();
        {
            var hasPipelinesReference = referenceList.Any(static r =>
                string.Equals(Path.GetFileNameWithoutExtension(r.FilePath), "System.IO.Pipelines", StringComparison.OrdinalIgnoreCase));
            if (!hasPipelinesReference)
            {
                var pipelinesAssemblyLocation = typeof(System.IO.Pipelines.PipeReader).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(pipelinesAssemblyLocation))
                {
                    referenceList.Add(MetadataReference.CreateFromFile(pipelinesAssemblyLocation));
                }
            }
        }
#if !NET
        {
            var memoryAssemblyLocation = typeof(Memory<>).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(memoryAssemblyLocation))
            {
                referenceList.Add(MetadataReference.CreateFromFile(memoryAssemblyLocation));
            }
        }
        {
            var asm = typeof(ValueTask).Assembly.Location;
            referenceList.Add(MetadataReference.CreateFromFile(asm));
        }
        {
            var asm = typeof(IAsyncEnumerable<>).Assembly.Location;
            referenceList.Add(MetadataReference.CreateFromFile(asm));
        }
#endif

        baseCompilation = CSharpCompilation.Create("generatortest",
            references: referenceList,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    CancellationToken TestCancellationToken => TestContext.CancellationTokenSource.Token;

    GeneratorDriver RunGenerators(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        IEnumerable<(string path, string content)>? additionalFiles = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
        => RunGenerators(source, out outputCompilation, out diagnostics, TestCancellationToken, additionalFiles, languageVersion);

    GeneratorDriver RunGenerators(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        CancellationToken cancellationToken,
        IEnumerable<(string path, string content)>? additionalFiles = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp12)
    {
        var parseOptions = new CSharpParseOptions(languageVersion);
        var generator = new MethodGenerator();
        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: additionalFiles?.Select(f =>
                (AdditionalText)new TestAdditionalText(f.path, f.content)) ?? [],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true)
        ).WithUpdatedParseOptions(parseOptions);

        var compilation = baseCompilation.AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(source, parseOptions, path: "input.cs",
                encoding: Encoding.UTF8, cancellationToken: cancellationToken));

        return driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics, cancellationToken);
    }

    Assembly Emit(Compilation compilation)
        => Emit(compilation, TestCancellationToken);

    Assembly Emit(Compilation compilation, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms, cancellationToken: cancellationToken);
        if (!result.Success)
        {
            foreach (var d in result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
                TestContext.WriteLine(d.ToString());
            foreach (var t in compilation.SyntaxTrees)
                TestContext.WriteLine($"// {t.FilePath}\n{t}");
            Assert.Fail("Compilation emit failed");
        }
        ms.Seek(0, SeekOrigin.Begin);

#if NET48
        return Assembly.Load(ms.ToArray());
#else
        var ctx = new System.Runtime.Loader.AssemblyLoadContext(nameof(FungeMethodGeneratorTests), isCollectible: true);
        return ctx.LoadFromStream(ms);
#endif
    }

    void AssertNoErrors(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
    {
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            foreach (var d in errors) TestContext.WriteLine(d.ToString());
            foreach (var t in compilation.SyntaxTrees) TestContext.WriteLine($"// {t.FilePath}\n{t}");
            Assert.Fail($"{errors.Length} error(s) in generator output");
        }
    }

    // -----------------------------------------------------------------------
    // Basic tests
    // -----------------------------------------------------------------------

    [TestMethod]
    public void EmptyProgram_Void_NoErrors()
    {
        // "@" is the Funge "stop" instruction — program terminates immediately
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
        Assert.AreEqual(4, comp.SyntaxTrees.Count()); // input.cs + attributes + helper + method
    }

    [TestMethod]
    public async Task HelloWorld_StringReturn()
    {
        // Classic Hello World in Funge-98
        const string helloWorld =
            "64+\"!dlroW ,olleH\",,,,,,,,,,,,,@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("hello.b98")]
                public static partial string Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("hello.b98", helloWorld)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (string?)m.Invoke(null, []);
            Assert.AreEqual("Hello, World!", result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task StringMode_SgmlStyleSpaces_StringReturn()
    {
        const string program = "\"   \"..@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("sgml.b98")]
                public static partial string Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("sgml.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (string?)m.Invoke(null, []);
            Assert.AreEqual("32 0 ", result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Iterate_K_ExecutesOperandCorrectly_StringReturn()
    {
        const string program = "2k6...@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("k.b98")]
                public static partial string Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("k.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (string?)m.Invoke(null, []);
            Assert.AreEqual("6 6 6 ", result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public void ReturnType_Void_TextWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(TextWriter output);
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_Task_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_Int_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_TaskInt_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task<int> Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_ValueTaskInt_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask<int> Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_TaskString_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task<string> Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_ValueTask_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_ValueTaskString_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask<string> Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_IEnumerableByte_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial IEnumerable<byte> Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void ReturnType_IAsyncEnumerableByte_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            using System.Threading;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial IAsyncEnumerable<byte> Run(CancellationToken cancellationToken = default);
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void Input_TextReader_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(TextReader input);
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    [TestMethod]
    public void Input_String_NoErrors()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(string input);
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")]);
        AssertNoErrors(diag, comp);
    }

    // -----------------------------------------------------------------------
    // Diagnostic tests
    // -----------------------------------------------------------------------

    [TestMethod]
    public void Diagnostic_InvalidReturnType_FG0002()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial double Run();
            }
            """;
        RunGenerators(source, out _, out var diag,
            additionalFiles: [("test.b98", "@")]);
        Assert.IsTrue(diag.Any(d => d.Id == "FG0002"), "Expected FG0002");
    }

    [TestMethod]
    public async Task Runtime_ExitCode_IntReturn_QReturnsStackTop()
    {
        const string program = "5q@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("exit-code.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("exit-code.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (int?)m.Invoke(null, []);
            Assert.AreEqual(5, result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_ExitCode_IntReturn_AtReturnsZero()
    {
        const string program = "@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("exit-code-zero.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("exit-code-zero.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (int?)m.Invoke(null, []);
            Assert.AreEqual(0, result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_3D_GoLow_ExitCode()
    {
        const string program = "l\f>7q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("go-low.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("go-low.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (int?)m.Invoke(null, []);
            Assert.AreEqual(7, result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_3D_GoHigh_ExitCode()
    {
        const string program = "h\f\f>7q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("go-high.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("go-high.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (int?)m.Invoke(null, []);
            Assert.AreEqual(7, result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_3D_HighLowIf_SelectsDirection()
    {
        const string programLow = "0m\f >1q\f >2q";
        const string programHigh = "1m\f >1q\f >2q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("select-low.b98")]
                public static partial int RunLow();

                [GenerateFungeMethod("select-high.b98")]
                public static partial int RunHigh();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("select-low.b98", programLow), ("select-high.b98", programHigh)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var mLow = t.GetMethod("RunLow")!;
            var mHigh = t.GetMethod("RunHigh")!;
            var low = (int?)mLow.Invoke(null, []);
            var high = (int?)mHigh.Invoke(null, []);
            Assert.AreEqual(1, low);
            Assert.AreEqual(2, high);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_3D_GetPut_UsesXYZ()
    {
        const string program = "88*1+500p500gq";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("getput-3d.b98")]
                public static partial int Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("getput-3d.b98", program)]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        await Task.Factory.StartNew(() =>
        {
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var result = (int?)m.Invoke(null, []);
            Assert.AreEqual(65, result);
        }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }

    [TestMethod]
    public async Task Runtime_FileInput_LoadsIntoSpace()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-gen-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            File.WriteAllText("input.txt", "A");

            var reversed = new string("input.txt".Reverse().ToArray());
            var program = $"00000\"{reversed}\"in000gq";

            var source = """
                using Esolang.Funge;
                namespace TestProject;
                partial class TestClass
                {
                    [GenerateFungeMethod("file-in.b98")]
                    public static partial int Run();
                }
                """;
            RunGenerators(source, out var comp, out var diag,
                additionalFiles: [("file-in.b98", program)]);
            AssertNoErrors(diag, comp);

            var asm = Emit(comp);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, []);
                Assert.AreEqual(65, result);
            }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public async Task Runtime_FileOutput_WritesRegion()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-gen-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var reversed = new string("output.txt".Reverse().ToArray());
            var program = $"88*1+000p00000000\"{reversed}\"o@";

            var source = """
                using Esolang.Funge;
                namespace TestProject;
                partial class TestClass
                {
                    [GenerateFungeMethod("file-out.b98")]
                    public static partial void Run();
                }
                """;
            RunGenerators(source, out var comp, out var diag,
                additionalFiles: [("file-out.b98", program)]);
            AssertNoErrors(diag, comp);

            var asm = Emit(comp);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                _ = m.Invoke(null, []);
            }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

            var bytes = File.ReadAllBytes(Path.Combine(tempDir, "output.txt"));
            CollectionAssert.AreEqual(new byte[] { 65 }, bytes);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void Diagnostic_SourceFileNotFound_FG0004()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("nonexistent.b98")]
                public static partial void Run();
            }
            """;
        RunGenerators(source, out _, out var diag);
        Assert.IsTrue(diag.Any(d => d.Id == "FG0004"), "Expected FG0004");
    }

    [TestMethod]
    public void Diagnostic_DuplicateInputParameter_FG0006()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(TextReader a, TextReader b);
            }
            """;
        RunGenerators(source, out _, out var diag,
            additionalFiles: [("test.b98", "@")]);
        Assert.IsTrue(diag.Any(d => d.Id == "FG0006"), "Expected FG0006");
    }

    [TestMethod]
    public void Diagnostic_ReturnOutputConflict_FG0007()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial string Run(TextWriter output);
            }
            """;
        RunGenerators(source, out _, out var diag,
            additionalFiles: [("test.b98", "@")]);
        Assert.IsTrue(diag.Any(d => d.Id == "FG0007"), "Expected FG0007");
    }

    [TestMethod]
    public void Runtime_SelfModifiedOutputWithoutOutputInterface_Throws()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "68*2-s<<@")]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        var t = asm.GetType("TestProject.TestClass")
            ?? asm.GetType("TestClass");
        Assert.IsNotNull(t, "Failed to find generated type TestProject.TestClass.");
        var m = t.GetMethod("Run", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        Assert.IsNotNull(m, "Failed to find generated method Run.");

        var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, []));
        Assert.IsNotNull(ex);
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
        Assert.Contains("without an output interface", ex.InnerException!.Message);
    }

    [TestMethod]
    public void Runtime_SelfModifiedInputWithoutInputInterface_Throws()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run();
            }
            """;
        RunGenerators(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "66*2+s<<@")]);
        AssertNoErrors(diag, comp);

        var asm = Emit(comp);
        var t = asm.GetType("TestProject.TestClass")
            ?? asm.GetType("TestClass");
        Assert.IsNotNull(t, "Failed to find generated type TestProject.TestClass.");
        var m = t.GetMethod("Run", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
        Assert.IsNotNull(m, "Failed to find generated method Run.");

        var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, []));
        Assert.IsNotNull(ex);
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType(ex.InnerException, typeof(InvalidOperationException));
        Assert.Contains("without an input interface", ex.InnerException!.Message);
    }

    // -----------------------------------------------------------------------
    // InlineSource with multiple lines
    // -----------------------------------------------------------------------

    [TestMethod]
    public void InlineSource_RawStringWithInnerQuotes_InspectGenerated()
    {
        // Inspect how the generator processes raw string literals in InlineSource
        // This test outputs the generated code to verify the processing
        var source = """"
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = """
                    @
                    """)]
                public static partial void Run();
            }
            """";
        RunGenerators(source, out var comp, out var diag);
        AssertNoErrors(diag, comp);

        var generated = comp.SyntaxTrees
            .Select(static t => t.ToString())
            .Single(static text => text.Contains("Generated from: <inline>", StringComparison.Ordinal));
        Assert.Contains("__cells[(0, 0, 0)] = 64;", generated);
        
        // Output all generated syntax trees for inspection
        TestContext.WriteLine("=== Generated Syntax Trees ===");
        foreach (var tree in comp.SyntaxTrees)
        {
            TestContext.WriteLine($"\n--- {tree.FilePath} ---");
            TestContext.WriteLine(tree.GetText().ToString());
        }
    }

    [TestMethod]
    public void InlineSource_MultiLine_BasicProgram()
    {
        // Verify multiline raw string is mapped to X/Y at Z=0 as expected.
        var source = """"
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = """
                    >v
                    ^@
                    """)]
                public static partial void Run();
            }
            """";
        RunGenerators(source, out var comp, out var diag);
        AssertNoErrors(diag, comp);

        var generated = comp.SyntaxTrees
            .Select(static t => t.ToString())
            .Single(static text => text.Contains("Generated from: <inline>", StringComparison.Ordinal));
        Assert.Contains("__cells[(0, 0, 0)] = 62;", generated); // '>'
        Assert.Contains("__cells[(1, 0, 0)] = 118;", generated); // 'v'
        Assert.Contains("__cells[(0, 1, 0)] = 94;", generated); // '^'
        Assert.Contains("__cells[(1, 1, 0)] = 64;", generated); // '@'
    }

    [TestMethod]
    public void InlineSource_WithEscapedNewlines()
    {
        // Test InlineSource with escaped newlines (\n) instead of literal raw strings
        // This avoids indentation issues with raw string literals
        var source = """"
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = "2:.@")]
                public static partial void Run();
            }
            """";
        RunGenerators(source, out var comp, out var diag);
        AssertNoErrors(diag, comp);
    }
}

/// <summary>Fake AdditionalText for testing.</summary>
file sealed class TestAdditionalText(string path, string content) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText? GetText(CancellationToken cancellationToken = default)
        => SourceText.From(content, Encoding.UTF8);
}
