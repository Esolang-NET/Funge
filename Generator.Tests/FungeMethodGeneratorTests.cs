using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;

namespace Esolang.Funge.Generator.Tests;

[TestClass]
public class FungeMethodGeneratorTests
{

    void LogWriteLine(string message) => TestContext.WriteLine(message);
#pragma warning disable MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する

    readonly Compilation baseCompilation = default!;

    readonly TestContext TestContext;

    public FungeMethodGeneratorTests(TestContext TestContext)
    {
        this.TestContext = TestContext;
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
            var hasLoggingReference = referenceList.Any(static r =>
                string.Equals(Path.GetFileNameWithoutExtension(r.FilePath), "Microsoft.Extensions.Logging.Abstractions", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetFileNameWithoutExtension(r.FilePath), "Microsoft.Extensions.Logging", StringComparison.OrdinalIgnoreCase));
            if (!hasLoggingReference)
            {
                var loggingAssemblyLocation = typeof(Microsoft.Extensions.Logging.ILogger).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(loggingAssemblyLocation))
                {
                    referenceList.Add(MetadataReference.CreateFromFile(loggingAssemblyLocation));
                }
            }
            var abstractionsAssemblyLocation = typeof(IFingerprint).Assembly.Location;
            if (!string.IsNullOrWhiteSpace(abstractionsAssemblyLocation))
            {
                referenceList.Add(MetadataReference.CreateFromFile(abstractionsAssemblyLocation));
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

    [TestMethod]
    public void TestGenerateWithStaticLoggerParameter()
    {
        var source = $$"""
            using Esolang.Funge;
            using Microsoft.Extensions.Logging;

            namespace TestNamespace;

            public partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = "@@")]
                public static partial void Run(ILogger<TestClass> logger);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatedSource = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));

            // Should use the parameter name directly, without 'this.' or field prefix
            Assert.Contains("logger: logger", generatedSource);
            Assert.DoesNotContain("logger: this.logger", generatedSource);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void TestGenerateWithNullableLoggerParameter()
    {
        var source = $$"""
            using Esolang.Funge;
            using Microsoft.Extensions.Logging;

            namespace TestNamespace;

            public partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = "@@")]
                public static partial void Run(ILogger? logger);
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            cancellationToken: CancellationToken);
        try
        {
            var runResult = driver.GetRunResult();
            var generatedSource = string.Join("\n", runResult.GeneratedTrees.Select(t => t.ToString()));

            Assert.Contains("logger: logger", generatedSource);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public async Task TestFunctionalLoggingInvocation()
    {
        var source = $$"""
            using Esolang.Funge;
            using Microsoft.Extensions.Logging;
            using System;
            using System.Collections.Generic;

            namespace TestNamespace;

            public class FakeLogger : ILogger
            {
                public List<string> Logs = new List<string>();
                public IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    Logs.Add(formatter(state, exception));
                }
            }

            public partial class TestClass
            {
                public FakeLogger Logger = new FakeLogger();

                [GenerateFungeMethod(InlineSource = "1 @")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source,
            out var outputCompilation, out var diagnostics,
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diagnostics, outputCompilation);

            var asm = Emit(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestNamespace.TestClass")!;
                var instance = Activator.CreateInstance(t)!;
                var m = t.GetMethod("Run")!;
                m.Invoke(instance, null);

                var loggerField = t.GetField("Logger")!;
                var logger = (dynamic)loggerField.GetValue(instance)!;
                var logs = (List<string>)logger.Logs;

                // Check if we have logs for '1' and '@'
                Assert.Contains(l => l.Contains("'1'"), logs);
                Assert.Contains(l => l.Contains("'@'"), logs);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public async Task TestFunctionalLoggingInvocation_PrimaryConstructor()
    {
        var source = $$"""
            using Esolang.Funge;
            using Microsoft.Extensions.Logging;
            using System;
            using System.Collections.Generic;

            namespace TestNamespace;

            public class FakeLogger : ILogger
            {
                public List<string> Logs = new List<string>();
                public IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    Logs.Add(formatter(state, exception));
                }
            }

            public partial class TestClass(FakeLogger logger)
            {
                [GenerateFungeMethod(InlineSource = "1 @")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source,
            out var outputCompilation, out var diagnostics,
            languageVersion: LanguageVersion.CSharp12,
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diagnostics, outputCompilation);

            var asm = Emit(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestNamespace.TestClass")!;
                var loggerType = asm.GetType("TestNamespace.FakeLogger")!;
                var loggerInstance = Activator.CreateInstance(loggerType)!;
                var instance = Activator.CreateInstance(t, loggerInstance)!;
                var m = t.GetMethod("Run")!;
                m.Invoke(instance, null);

                var logs = (List<string>)loggerType.GetField("Logs")!.GetValue(loggerInstance)!;

                Assert.Contains(l => l.Contains("'1'"), logs);
                Assert.Contains(l => l.Contains("'@'"), logs);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation);
            throw;
        }
    }

    [TestMethod]
    public async Task TestFunctionalFingerprintLogging()
    {
        // 0x524f4d41 = 'ROMA'
        // 'y' (121) pushes sysinfo
        // '(' (40) loads fingerprint
        // ')' (41) unloads fingerprint
        var source = $$"""
            using Esolang.Funge;
            using Microsoft.Extensions.Logging;
            using System;
            using System.Collections.Generic;

            namespace TestNamespace;

            public class FakeLogger : ILogger
            {
                public List<string> Logs = new List<string>();
                public IDisposable BeginScope<TState>(TState state) => null!;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
                {
                    Logs.Add(formatter(state, exception));
                }
            }

            public partial class TestClass
            {
                public FakeLogger Logger = new FakeLogger();

                [GenerateFungeMethod(InlineSource = "1y \"ROMA\" 4( \"AMOR\" 4) @")]
                public partial void Run();
            }
            """;

        var driver = RunGeneratorsAndUpdateCompilation(source,
            out var outputCompilation, out var diagnostics,
            languageVersion: LanguageVersion.CSharp12,
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diagnostics, outputCompilation);

            var asm = Emit(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestNamespace.TestClass")!;
                var instance = Activator.CreateInstance(t)!;
                var m = t.GetMethod("Run")!;
                m.Invoke(instance, null);

                var loggerField = t.GetField("Logger")!;
                var logger = (dynamic)loggerField.GetValue(instance)!;
                var logs = (List<string>)logger.Logs;

                LogWriteLine("Actual Logs:\n" + string.Join("\n", logs));

                // Check for System Information log
                Assert.Contains(l => l.Contains("System information requested") && l.Contains("Argument: 1"), logs);

                // Check for Fingerprint Loaded log ('AMOR' because we pushed 'ROMA' and pop 4 times)
                Assert.Contains("IP 0: Fingerprint 'AMOR' loaded", logs);

                // Check for Fingerprint Unloaded log ('ROMA' because we pushed 'AMOR' and pop 4 times)
                Assert.Contains("IP 0: Fingerprint 'ROMA' unloaded", logs);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation);
            throw;
        }
    }


    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        IEnumerable<(string path, string content)>? additionalFiles = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp11,
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

    Assembly Emit(Compilation compilation, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms, cancellationToken: cancellationToken);
        Assert.IsTrue(result.Success);
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
            foreach (var d in errors) LogWriteLine(d.ToString());
            foreach (var t in compilation.SyntaxTrees) LogWriteLine($"// {t.FilePath}\n{t}");
            Assert.Fail($"{errors.Length} error(s) in generator output");
        }
    }
    void LogDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var d in diagnostics)
            LogWriteLine(d.ToString());
    }

    void LogDiagnostics(Compilation compilation)
    {
        foreach (var d in compilation.GetDiagnostics(CancellationToken))
            LogWriteLine(d.ToString());
    }
    void LogDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
    {
        LogDiagnostics(diagnostics);
        LogDiagnostics(compilation);
        LogSyntaxTrees(compilation);
    }
    void LogSyntaxTrees(Compilation compilation)
    {
        foreach (var t in compilation.SyntaxTrees)
            LogWriteLine($"// {t.FilePath}\n{t}");
    }

    static async Task<string> ReadPipeOutputAsync(Pipe pipe)
    {
        await pipe.Writer.CompleteAsync();
        using var reader = new StreamReader(pipe.Reader.AsStream());
        var text = await reader.ReadToEndAsync();
        await pipe.Reader.CompleteAsync();
        return text;
    }

    // -----------------------------------------------------------------------
    // Basic tests
    // -----------------------------------------------------------------------

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
            var actualPaths = comp.SyntaxTrees.Select(v => v.FilePath).ToArray();

            // Ensure expected files are present regardless of exact order or separator style
            var expectedFiles = new[] { "input.cs", "GenerateFungeMethodAttribute.cs", "GenerateFungeMethod.g.cs" };
            foreach (var expected in expectedFiles)
            {
                Assert.Contains(p => p.Contains(expected, StringComparison.OrdinalIgnoreCase), actualPaths, $"Missing file: {expected}");
            }
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
                public static partial string Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("hello.b98", helloWorld)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (string?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual("Hello, World!", result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task StringMode_SgmlStyleSpaces_StringReturn()
    {
        const string program = "\"   \"..@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("sgml.b98")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("sgml.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (string?)m.Invoke(null, [CancellationToken])!;
                Assert.AreEqual("32 0 ", result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Iterate_K_ExecutesOperandCorrectly_StringReturn()
    {
        const string program = "2k6...@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("k.b98")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("k.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (string?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual("6 6 6 ", result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_Int_TextWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial int Run(TextWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_TaskInt_TextWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task<int> Run(TextWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_ValueTaskInt_TextWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask<int> Run(TextWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_Int_PipeWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial int Run(PipeWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_TaskInt_PipeWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task<int> Run(PipeWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void ReturnType_ValueTaskInt_PipeWriter()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask<int> Run(PipeWriter output);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void RuntimeTemplate_NullabilityWarnings_NotEmitted()
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var options = (CSharpCompilationOptions)comp.Options;
            var strict = comp.WithOptions(options.WithSpecificDiagnosticOptions(
                options.SpecificDiagnosticOptions
                    .SetItem("CS8602", ReportDiagnostic.Error)
                    .SetItem("CS8603", ReportDiagnostic.Error)));

            var runtimeNullabilityErrors = strict
                .GetDiagnostics(CancellationToken)
                .Where(static d => d.Severity == DiagnosticSeverity.Error)
                .Where(static d => d.Id is "CS8602" or "CS8603")
                .Where(static d => d.Location.SourceTree?.FilePath.Contains("FungeRuntime.g.cs", StringComparison.OrdinalIgnoreCase) == true)
                .ToArray();

            if (runtimeNullabilityErrors.Length > 0)
            {
                foreach (var d in runtimeNullabilityErrors)
                    LogWriteLine(d.ToString());
            }

            Assert.IsEmpty(runtimeNullabilityErrors, "FungeRuntime.g.cs must not produce CS8602/CS8603.");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            Assert.Contains(d => d.Id == "FG0002", diag, "Expected FG0002");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_ExitCode_IntReturn_QReturnsStackTop()
    {
        const string program = "5q@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("exit-code.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("exit-code.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(5, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_ExitCode_IntReturn_AtReturnsZero()
    {
        const string program = "@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("exit-code-zero.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("exit-code-zero.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(0, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_3D_GoLow_ExitCode()
    {
        const string program = "l\f>7q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("go-low.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("go-low.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(7, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_3D_GoHigh_ExitCode()
    {
        const string program = "h\f\f>7q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("go-high.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("go-high.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(7, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
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
                public static partial int RunLow(System.Threading.CancellationToken cancellationToken);

                [GenerateFungeMethod("select-high.b98")]
                public static partial int RunHigh(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("select-low.b98", programLow), ("select-high.b98", programHigh)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var mLow = t.GetMethod("RunLow");
                Assert.IsNotNull(mLow);
                var mHigh = t.GetMethod("RunHigh");
                Assert.IsNotNull(mHigh);
                var low = (int?)mLow.Invoke(null, [CancellationToken]);
                var high = (int?)mHigh.Invoke(null, [CancellationToken]);
                Assert.AreEqual(1, low);
                Assert.AreEqual(2, high);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_3D_GetPut_UsesXYZ()
    {
        const string program = "88*1+500p500gq";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("getput-3d.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("getput-3d.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(65, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_StorageOffset_AppliesToGetPut()
    {
        const string program = "0{88*1+000p000gq";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("offset-getput.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("offset-getput.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(65, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_StackStack_U_TransfersFromSoss()
    {
        const string program = "120{4u.@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("stack-u.b98")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("stack-u.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            // Capture the compilation
            var asm = Emit(comp, CancellationToken);

            // Print generated code for inspection
            var syntaxTrees = comp.SyntaxTrees;
            foreach (var tree in syntaxTrees)
            {
                if (tree.FilePath.EndsWith("GenerateFungeMethod.g.cs"))
                {
                    // Console.WriteLine(tree.ToString());
                }
            }

            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run", [typeof(CancellationToken)])!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual("2 ", result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_SystemInfo_FlagsIncludesConcurrentFileExec()
    {
        const string program = "1yq";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("sysinfo-flags.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("sysinfo-flags.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual(15, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_FileInput_LoadsIntoSpace()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-gen-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);
            File.WriteAllText("input.txt", "A");

            var reversed = new string([.. "input.txt".Reverse()]);
            var program = $"00000\"{reversed}\"in000gq";

            var source = """
                using Esolang.Funge;
                namespace TestProject;
                partial class TestClass
                {
                    [GenerateFungeMethod("file-in.b98")]
                    public static partial int Run(System.Threading.CancellationToken cancellationToken);
                }
                """;
            RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
                additionalFiles: [("file-in.b98", program)],
                cancellationToken: CancellationToken);
            try
            {
                AssertNoErrors(diag, comp);

                var asm = Emit(comp, CancellationToken);
                await Task.Factory.StartNew(() =>
                {
                    var t = asm.GetType("TestProject.TestClass")!;
                    var m = t.GetMethod("Run")!;
                    var result = (int?)m.Invoke(null, [CancellationToken]);
                    Assert.AreEqual(65, result);
                }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
            {
                LogDiagnostics(diag, comp);
                throw;
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_FileOutput_WritesRegion()
    {
        var originalDir = Directory.GetCurrentDirectory();
        var tempDir = Path.Combine(Path.GetTempPath(), $"funge-gen-io-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            Directory.SetCurrentDirectory(tempDir);

            var reversed = new string([.. "output.txt".Reverse()]);
            var program = $"88*1+000p00000000\"{reversed}\"o@";

            var source = """
                using Esolang.Funge;
                namespace TestProject;
                partial class TestClass
                {
                    [GenerateFungeMethod("file-out.b98")]
                    public static partial void Run(System.Threading.CancellationToken cancellationToken);
                }
                """;
            RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
                additionalFiles: [("file-out.b98", program)],
                cancellationToken: CancellationToken);
            try
            {
                AssertNoErrors(diag, comp);

                var asm = Emit(comp, CancellationToken);
                await Task.Factory.StartNew(() =>
                {
                    var t = asm.GetType("TestProject.TestClass")!;
                    var m = t.GetMethod("Run")!;
                    _ = m.Invoke(null, [CancellationToken]);
                }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);

                var bytes = File.ReadAllBytes(Path.Combine(tempDir, "output.txt"));
                CollectionAssert.AreEqual(new byte[] { 65 }, bytes);
            }
            catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
            {
                LogDiagnostics(diag, comp);
                throw;
            }
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDir);
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_SystemExec_ReturnsExitCode()
    {
        const string command = "exit 7";
        var reversed = new string([.. command.Reverse()]);
        var program = $"0\"{reversed}\"=q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("system-exec.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("system-exec.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int)((int?)m.Invoke(null, [CancellationToken]))!;
                Assert.AreEqual(7, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_SystemExec_FailureIsNonZero()
    {
        const string command = "this_command_should_not_exist_12345";
        var reversed = new string([.. command.Reverse()]);
        var program = $"0\"{reversed}\"=q";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("system-exec-fail.b98")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("system-exec-fail.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                Assert.AreNotEqual(0, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void Generated_TaskReturn_UsesTaskRuntimeFacade()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("task-facade.b98")]
                public static partial Task Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("task-facade.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var generated = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("Generated from: task-facade.b98", StringComparison.Ordinal));
            Assert.Contains("FungeRuntime.RunTask(", generated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void Generated_ValueTaskStringReturn_UsesValueTaskRuntimeFacade()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("valuetask-facade.b98")]
                public static partial ValueTask<string> Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("valuetask-facade.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var generated = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("Generated from: valuetask-facade.b98", StringComparison.Ordinal));
            Assert.Contains("FungeRuntime.RunValueTaskString(", generated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_AsyncEnumerableByte_ReturnsOutputBytes()
    {
        const string program = "\"A\",@";

        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            using System.Threading;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("async-enumerable.b98")]
                public static partial IAsyncEnumerable<byte> Run(CancellationToken cancellationToken = default);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("async-enumerable.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var stream = (IAsyncEnumerable<byte>?)m.Invoke(null, [CancellationToken]);
            Assert.IsNotNull(stream);

            var bytes = new List<byte>();
            var enumerator = stream!.GetAsyncEnumerator(CancellationToken);
            try
            {
                while (await enumerator.MoveNextAsync())
                    bytes.Add(enumerator.Current);
            }
            finally
            {
                await enumerator.DisposeAsync();
            }

            CollectionAssert.AreEqual(new byte[] { (byte)'A' }, bytes);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Generated_SyncWithCancellationToken_UsesRunSyncWithToken()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("sync-token.b98")]
                public static partial int Run(CancellationToken cancellationToken = default);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("sync-token.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var generated = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("Generated from: sync-token.b98", StringComparison.Ordinal));
            Assert.Contains("FungeRuntime.RunSync(", generated);
            Assert.Contains("cancellationToken", generated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void Generated_Runtime_EmitsOnlyRequiredFacades_ForIntReturn()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("minimal-int.b98")]
                public static partial int Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("minimal-int.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var runtime = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("internal static class FungeRuntime", StringComparison.Ordinal));

            Assert.Contains("internal static int RunSync(", runtime);
            Assert.IsFalse(runtime.Contains("internal static Task RunTask(", StringComparison.Ordinal));
            Assert.IsFalse(runtime.Contains("internal static ValueTask<string> RunValueTaskString(", StringComparison.Ordinal));
            Assert.IsFalse(runtime.Contains("internal static async IAsyncEnumerable<byte> RunAsyncEnumerable(", StringComparison.Ordinal));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void Generated_Runtime_EmitsOnlyRequiredFacades_ForValueTaskString()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("minimal-vts.b98")]
                public static partial ValueTask<string> Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("minimal-vts.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var runtime = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("internal static class FungeRuntime", StringComparison.Ordinal));

            Assert.Contains("internal static ValueTask<string> RunValueTaskString(", runtime);
            Assert.IsFalse(runtime.Contains("internal static int RunSync(", StringComparison.Ordinal));
            Assert.IsFalse(runtime.Contains("internal static Task<int> RunTaskInt(", StringComparison.Ordinal));
            Assert.IsFalse(runtime.Contains("internal static IEnumerable<byte> RunEnumerable(", StringComparison.Ordinal));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Runtime_SyncCancellationToken_CancelsInfiniteLoop()
    {
        var source = """
            using Esolang.Funge;
            using System.Threading;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("infinite-loop.b98")]
                public static partial int Run(CancellationToken cancellationToken = default);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("infinite-loop.b98", ">")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            Assert.IsNotNull(m);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = Assert.Throws<TargetInvocationException>(() => m.Invoke(null, [cts.Token]));
            Assert.IsNotNull(ex?.InnerException);
            Assert.IsInstanceOfType(ex!.InnerException, typeof(OperationCanceledException));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            Assert.IsTrue(diag.Any(d => d.Id == "FG0004"), "Expected FG0004");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            Assert.IsTrue(diag.Any(d => d.Id == "FG0006"), "Expected FG0006");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "@")],
            cancellationToken: CancellationToken);
        try
        {
            Assert.IsTrue(diag.Any(d => d.Id == "FG0007"), "Expected FG0007");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_Int_TextWriter_ReturnsExitCodeAndWritesOutput()
    {
        var source = """
            using Esolang.Funge;
            using System.IO;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial int Run(TextWriter output, System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "65*.5q")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                using var output = new StringWriter();
                var result = (int)m.Invoke(null, [output, CancellationToken])!;
                Assert.AreEqual(5, result);
                Assert.AreEqual("30 ", output.ToString());
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_Int_PipeWriter_ReturnsExitCodeAndWritesOutput()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial int Run(PipeWriter output, System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "65*.5q")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.IsNotNull(m);
            var pipe = new Pipe();
            var result = (int)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            Assert.AreEqual(5, result);
            Assert.AreEqual("30 ", await ReadPipeOutputAsync(pipe));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_TaskInt_PipeWriter_ReturnsExitCodeAndWritesOutput()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial Task<int> Run(PipeWriter output, System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "65*.5q")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.IsNotNull(m);
            var pipe = new Pipe();
            var result = await (Task<int>)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            Assert.AreEqual(5, result);
            Assert.AreEqual("30 ", await ReadPipeOutputAsync(pipe));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_ValueTaskInt_PipeWriter_ReturnsExitCodeAndWritesOutput()
    {
        var source = """
            using Esolang.Funge;
            using System.IO.Pipelines;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial ValueTask<int> Run(PipeWriter output, System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "65*.5q")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.IsNotNull(m);
            var pipe = new Pipe();
            var result = await (ValueTask<int>)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            Assert.AreEqual(5, result);
            Assert.AreEqual("30 ", await ReadPipeOutputAsync(pipe));
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Runtime_SelfModifiedOutputWithoutOutputInterface_Throws()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "68*2-s<<@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")
                ?? asm.GetType("TestClass");
            Assert.IsNotNull(t, "Failed to find generated type TestProject.TestClass.");
            var m = t.GetMethod("Run");
            Assert.IsNotNull(m, "Failed to find generated method Run.");

            var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, [CancellationToken]));
            Assert.IsNotNull(ex);
            Assert.IsNotNull(ex.InnerException);
            Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("without an output interface", ex.InnerException!.Message);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public void Runtime_SelfModifiedInputWithoutInputInterface_Throws()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("test.b98")]
                public static partial void Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("test.b98", "66*2+s<<@")],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")
                ?? asm.GetType("TestClass");
            Assert.IsNotNull(t, "Failed to find generated type TestProject.TestClass.");
            var m = t.GetMethod("Run");
            Assert.IsNotNull(m, "Failed to find generated method Run.");

            var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, [CancellationToken]));
            Assert.IsNotNull(ex);
            Assert.IsNotNull(ex.InnerException);
            Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
            Assert.Contains("without an input interface", ex.InnerException!.Message);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var generated = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("Generated from: <inline>", StringComparison.Ordinal));
            Assert.Contains("__cells[(0, 0, 0)] = 64;", generated);

            // Output all generated syntax trees for inspection
            LogWriteLine("=== Generated Syntax Trees ===");
            foreach (var tree in comp.SyntaxTrees)
            {
                LogWriteLine($"\n--- {tree.FilePath} ---");
                LogWriteLine(tree.GetText(TestContext.CancellationToken).ToString());
            }
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var generated = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("Generated from: <inline>", StringComparison.Ordinal));
            Assert.Contains("__cells[(0, 0, 0)] = 62;", generated); // '>'
            Assert.Contains("__cells[(1, 0, 0)] = 118;", generated); // 'v'
            Assert.Contains("__cells[(0, 1, 0)] = 94;", generated); // '^'
            Assert.Contains("__cells[(1, 1, 0)] = 64;", generated); // '@'
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
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
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }
    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task Runtime_SystemInfo_ReportsCustomArgs()
    {
        // Verify that the generated method can accept string[] args and string[] envs
        // and that they are correctly passed to the runtime.
        const string program = "@";

        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("args-test.b98")]
                public static partial int Run(string[] args, string[] envs, System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag,
            additionalFiles: [("args-test.b98", program)],
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.IsNotNull(m);
                // Verify we can call it with the new parameters
                var result = (int?)m.Invoke(null, [new[] { "test" }, new[] { "VAR=VAL" }, CancellationToken]);
                Assert.AreEqual(0, result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // FingerprintsProvider tests
    // -----------------------------------------------------------------------

    [TestMethod]
    public void FingerprintsProvider_Property_GeneratesArgument()
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public IEnumerable<IFingerprint> MyFingerprints => System.Array.Empty<IFingerprint>();

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "MyFingerprints")]
                public partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var allGenerated = string.Join("\n", comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Where(static text => text.Contains("Generated from:", StringComparison.Ordinal)));
            Assert.Contains("fingerprints:", allGenerated);
            Assert.Contains("this.MyFingerprints", allGenerated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void FingerprintsProvider_Method_GeneratesArgument()
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public IEnumerable<IFingerprint> GetFingerprints() => System.Array.Empty<IFingerprint>();

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "GetFingerprints")]
                public partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var allGenerated = string.Join("\n", comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Where(static text => text.Contains("Generated from:", StringComparison.Ordinal)));
            Assert.Contains("fingerprints:", allGenerated);
            Assert.Contains("this.GetFingerprints()", allGenerated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void FingerprintsProvider_StaticMethod_GeneratesQualifiedExpression()
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints() => System.Array.Empty<IFingerprint>();

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "GetFingerprints")]
                public static partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var allGenerated = string.Join("\n", comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Where(static text => text.Contains("Generated from:", StringComparison.Ordinal)));
            Assert.Contains("fingerprints:", allGenerated);
            // Static member reference is fully qualified with global:: prefix
            Assert.Contains("global::TestProject.TestClass.GetFingerprints()", allGenerated);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void FingerprintsProvider_InvalidMember_EmitsFG0012()
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "NonExistentMember")]
                public partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            Assert.IsTrue(diag.Any(d => d.Id == "FG0012"), "Expected FG0012 for unknown FingerprintsProvider member");
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    public void FingerprintsProvider_EnablesFingerprintSupportRuntime()
    {
        // When FingerprintsProvider is set, the generated runtime should include RuntimeFungeExecutionContext.
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints() => System.Array.Empty<IFingerprint>();

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var runtime = string.Join("\n", comp.SyntaxTrees.Select(static t => t.ToString()));
            Assert.Contains("RuntimeFungeExecutionContext", runtime);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }

    [TestMethod]
    [Timeout(Constant.Timeout, CooperativeCancellation = true)]
    public async Task FingerprintsProvider_Functional_DispatchesInstruction()
    {
        // Integration test: FingerprintsProvider wires up a real fingerprint.
        // PEST fingerprint with 'A' → pushes 42, program outputs it as integer.
        // Program: "TSEP"4(A.@ (load PEST, run A which pushes 42, output, stop)
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new PestFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(A.@", FingerprintsProvider = "GetFingerprints")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);

                sealed class PestFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }
                        = new Dictionary<char, FingerprintInstruction> { ['A'] = ctx => ctx.Push(42) };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = Emit(comp, CancellationToken);
            await Task.Factory.StartNew(() =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                Assert.AreEqual("42 ", result);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertFailedException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp);
            throw;
        }
    }
}

/// <summary>Fake AdditionalText for testing.</summary>
file sealed class TestAdditionalText(string path, string content) : AdditionalText
{
    public override string Path { get; } = path;
    public override SourceText? GetText(CancellationToken cancellationToken = default)
        => SourceText.From(content, Encoding.UTF8);
}


file static class Constant
{
    public const int Timeout = 1000 * 30;
}
