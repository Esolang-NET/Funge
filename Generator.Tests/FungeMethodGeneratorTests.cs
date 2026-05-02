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
#else
            Net80.References.All;
#endif
        baseCompilation = CSharpCompilation.Create("generatortest",
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    GeneratorDriver RunGenerators(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        IEnumerable<(string path, string content)>? additionalFiles = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp12,
        CancellationToken cancellationToken = default)
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

    (System.Runtime.Loader.AssemblyLoadContext Ctx, Assembly Assembly) Emit(Compilation compilation, CancellationToken cancellationToken = default)
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
        var ctx = new System.Runtime.Loader.AssemblyLoadContext(nameof(FungeMethodGeneratorTests), isCollectible: true);
        var asm = ctx.LoadFromStream(ms);
        return (ctx, asm);
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

        var (ctx, asm) = Emit(comp);
        try
        {
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, []);
                Assert.AreEqual("Hello, World!", result);
            }, TestContext.CancellationTokenSource.Token, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        finally { ctx.Unload(); }
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
                public static partial int Run();
            }
            """;
        RunGenerators(source, out _, out var diag,
            additionalFiles: [("test.b98", "@")]);
        Assert.IsTrue(diag.Any(d => d.Id == "FG0002"), "Expected FG0002");
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
}

/// <summary>Fake AdditionalText for testing.</summary>
file sealed class TestAdditionalText(string path, string content) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText? GetText(CancellationToken cancellationToken = default)
        => SourceText.From(content, Encoding.UTF8);
}
