using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.IO.Pipelines;
using System.Reflection;
using System.Text;
using TUnit.Assertions.Enums;
using TUnit.Assertions.Exceptions;

namespace Esolang.Funge.Generator.Tests;

public class FungeMethodGeneratorTests
{

    static int AssemblySequence;

    void LogWriteLine(string message) => TestContext.OutputWriter.WriteLine(message);

    readonly Compilation baseCompilation = default!;

    readonly TestContext TestContext;

    public FungeMethodGeneratorTests()
    {
        TestContext = TestContext.Current!;
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
                var pipelinesAssemblyLocation = typeof(PipeReader).Assembly.Location;
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

    [Test]
    public async Task TestGenerateWithStaticLoggerParameter(CancellationToken CancellationToken)
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
            await Assert.That(generatedSource).Contains("logger: logger");
            await Assert.That(generatedSource).DoesNotContain("logger: this.logger");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task TestGenerateWithNullableLoggerParameter(CancellationToken CancellationToken)
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

            await Assert.That(generatedSource).Contains("logger: logger");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task TestFunctionalLoggingInvocation(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestNamespace.TestClass")!;
                var instance = Activator.CreateInstance(t)!;
                var m = t.GetMethod("Run")!;
                m.Invoke(instance, null);

                var loggerField = t.GetField("Logger")!;
                var logger = (dynamic)loggerField.GetValue(instance)!;
                var logs = (List<string>)logger.Logs;

                // Check if we have logs for '1' and '@'
                await Assert.That(logs).Contains(l => l.Contains("'1'"));
                await Assert.That(logs).Contains(l => l.Contains("'@'"));
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task TestFunctionalLoggingInvocation_PrimaryConstructor(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestNamespace.TestClass")!;
                var loggerType = asm.GetType("TestNamespace.FakeLogger")!;
                var loggerInstance = Activator.CreateInstance(loggerType)!;
                var instance = Activator.CreateInstance(t, loggerInstance)!;
                var m = t.GetMethod("Run")!;
                m.Invoke(instance, null);

                var logs = (List<string>)loggerType.GetField("Logs")!.GetValue(loggerInstance)!;

                await Assert.That(logs).Contains(l => l.Contains("'1'"));
                await Assert.That(logs).Contains(l => l.Contains("'@'"));
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task TestFunctionalFingerprintLogging(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(outputCompilation, CancellationToken);
            await Task.Factory.StartNew(async () =>
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
                await Assert.That(logs).Contains(l => l.Contains("System information requested") && l.Contains("Argument: 1"));

                // Check for Fingerprint Loaded log ('AMOR' because we pushed 'ROMA' and pop 4 times)
                await Assert.That(logs).Contains("IP 0: Fingerprint 'AMOR' loaded");

                // Check for Fingerprint Unloaded log ('ROMA' because we pushed 'AMOR' and pop 4 times)
                await Assert.That(logs).Contains("IP 0: Fingerprint 'ROMA' unloaded");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diagnostics, outputCompilation, CancellationToken);
            throw;
        }
    }


    GeneratorDriver RunGeneratorsAndUpdateCompilation(
        string source,
        out Compilation outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics,
        IEnumerable<(string path, string content)>? additionalFiles = null,
        LanguageVersion languageVersion = LanguageVersion.CSharp11,
        bool includeFungeAbstractionsReference = true,
        Dictionary<string, ReportDiagnostic>? specificDiagnosticOptions = null,
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
        var compilation = baseCompilation;
        if (specificDiagnosticOptions is not null)
        {
            var options = (CSharpCompilationOptions)compilation.Options;
            options = options.WithSpecificDiagnosticOptions(options.SpecificDiagnosticOptions.SetItems(specificDiagnosticOptions));
            compilation = compilation.WithOptions(options);
        }
        compilation = (includeFungeAbstractionsReference
                ? compilation
                : compilation.RemoveReferences(compilation.References.Where(static reference =>
                string.Equals(reference.Display, typeof(IFingerprint).Assembly.Location, StringComparison.OrdinalIgnoreCase))))
            .WithAssemblyName($"generatortest_{Interlocked.Increment(ref AssemblySequence)}")
            .AddSyntaxTrees(
            CSharpSyntaxTree.ParseText(source, parseOptions, path: "input.cs",
                 encoding: Encoding.UTF8, cancellationToken: cancellationToken));


        return driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics, cancellationToken);
    }

    static async Task<Assembly> EmitAsync(Compilation compilation, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms, cancellationToken: cancellationToken);
        await Assert.That(result.Success).IsTrue();
        ms.Seek(0, SeekOrigin.Begin);

#if NET48
        return System.Reflection.Assembly.Load(ms.ToArray());
#else
        var ctx = new System.Runtime.Loader.AssemblyLoadContext(
            $"{nameof(FungeMethodGeneratorTests)}_{compilation.AssemblyName}",
            isCollectible: true);
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

    void LogDiagnostics(Compilation compilation, CancellationToken CancellationToken)
    {
        foreach (var d in compilation.GetDiagnostics(CancellationToken))
            LogWriteLine(d.ToString());
    }
    void LogDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation, CancellationToken CancellationToken)
    {
        LogDiagnostics(diagnostics);
        LogDiagnostics(compilation, CancellationToken);
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

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task EmptyProgram_Void_NoErrors(CancellationToken CancellationToken)
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
                await Assert.That(actualPaths).Contains(p => p.Contains(expected, StringComparison.OrdinalIgnoreCase)).Because($"Missing file: {expected}");
            }
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task HelloWorld_StringReturn(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("Hello, World!");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task StringMode_SgmlStyleSpaces_StringReturn(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (string?)m.Invoke(null, [CancellationToken])!;
                await Assert.That(result).IsEqualTo("32 0 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Iterate_K_ExecutesOperandCorrectly_StringReturn(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("6 6 6 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_Void_TextWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_Int_TextWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_TaskInt_TextWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_ValueTaskInt_TextWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_Int_PipeWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_TaskInt_PipeWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_ValueTaskInt_PipeWriter(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_Task_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_Int_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_TaskInt_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_ValueTaskInt_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_TaskString_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_ValueTask_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_ValueTaskString_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_IEnumerableByte_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void ReturnType_IAsyncEnumerableByte_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void Input_TextReader_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void Input_String_NoErrors(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task RuntimeTemplate_NullabilityWarnings_NotEmitted(CancellationToken CancellationToken)
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

            await Assert.That(runtimeNullabilityErrors).IsEmpty().Because("FungeRuntime.g.cs must not produce CS8602/CS8603.");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // Diagnostic tests
    // -----------------------------------------------------------------------

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Diagnostic_InvalidReturnType_FG0002(CancellationToken CancellationToken)
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
            await Assert.That(diag).Any(d => d.Id == "FG0002").Because("Expected FG0002");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_ExitCode_IntReturn_QReturnsStackTop(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(5);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_ExitCode_IntReturn_AtReturnsZero(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(0);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_3D_GoLow_ExitCode(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(7);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_3D_GoHigh_ExitCode(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(7);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_3D_HighLowIf_SelectsDirection(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var mLow = t.GetMethod("RunLow");
                await Assert.That(mLow).IsNotNull();
                var mHigh = t.GetMethod("RunHigh");
                await Assert.That(mHigh).IsNotNull();
                var low = (int?)mLow.Invoke(null, [CancellationToken]);
                var high = (int?)mHigh.Invoke(null, [CancellationToken]);
                await Assert.That(low).IsEqualTo(1);
                await Assert.That(high).IsEqualTo(2);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_3D_GetPut_UsesXYZ(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(65);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_StorageOffset_AppliesToGetPut(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                await Assert.That(m).IsNotNull();
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(65);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_StackStack_U_TransfersFromSoss(CancellationToken CancellationToken)
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
            var asm = await EmitAsync(comp, CancellationToken);

            // Print generated code for inspection
            var syntaxTrees = comp.SyntaxTrees;
            foreach (var tree in syntaxTrees)
            {
                if (tree.FilePath.EndsWith("GenerateFungeMethod.g.cs"))
                {
                    // Console.WriteLine(tree.ToString());
                }
            }

            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("2 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SystemInfo_FlagsIncludesConcurrentFileExec(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(15);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_FileInput_LoadsIntoSpace(CancellationToken CancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), TestContext.Isolation.GetIsolatedName($"funge-gen-file-input-{Guid.NewGuid():N}"));
        var inputPath = Path.Combine(tempDir, "input.txt");
        Directory.CreateDirectory(tempDir);

        try
        {
            File.WriteAllText(inputPath, "A");

            var reversed = new string([.. inputPath.Reverse()]);
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

                var asm = await EmitAsync(comp, CancellationToken);
                await Task.Factory.StartNew(async () =>
                {
                    var t = asm.GetType("TestProject.TestClass")!;
                    var m = t.GetMethod("Run")!;
                    var result = (int?)m.Invoke(null, [CancellationToken]);
                    await Assert.That(result).IsEqualTo(65);
                }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
            catch (Exception e) when (e is AssertionException or TargetInvocationException)
            {
                LogDiagnostics(diag, comp, CancellationToken);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch { }
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_FileOutput_WritesRegion(CancellationToken CancellationToken)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), TestContext.Isolation.GetIsolatedName($"funge-gen-file-output-{Guid.NewGuid():N}"));
        var outputPath = Path.Combine(tempDir, "output.txt");
        Directory.CreateDirectory(tempDir);

        try
        {
            var reversed = new string([.. outputPath.Reverse()]);
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

                var asm = await EmitAsync(comp, CancellationToken);
                await Task.Factory.StartNew(async () =>
                {
                    var t = asm.GetType("TestProject.TestClass")!;
                    var m = t.GetMethod("Run")!;
                    _ = m.Invoke(null, [CancellationToken]);
                }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
                CancellationToken.ThrowIfCancellationRequested();
                var bytes = File.ReadAllBytes(outputPath);
                await Assert.That(bytes).IsEquivalentTo((byte[])[65], CollectionOrdering.Matching);
            }
            catch (Exception e) when (e is AssertionException or TargetInvocationException)
            {
                LogDiagnostics(diag, comp, CancellationToken);
                throw;
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                try
                {
                    Directory.Delete(tempDir, recursive: true);
                }
                catch { }
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SystemExec_ReturnsExitCode(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int)((int?)m.Invoke(null, [CancellationToken]))!;
                await Assert.That(result).IsEqualTo(7);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SystemExec_FailureIsNonZero(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsNotEqualTo(0);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generated_TaskReturn_UsesTaskRuntimeFacade(CancellationToken CancellationToken)
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
            await Assert.That(generated).Contains("FungeRuntime.RunTask(");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generated_ValueTaskStringReturn_UsesValueTaskRuntimeFacade(CancellationToken CancellationToken)
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
            await Assert.That(generated).Contains("FungeRuntime.RunValueTaskString(");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_AsyncEnumerableByte_ReturnsOutputBytes(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            var stream = (IAsyncEnumerable<byte>?)m.Invoke(null, [CancellationToken]);
            Assert.NotNull(stream);

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

            await Assert.That(bytes).IsEquivalentTo((byte[])[(byte)'A'], CollectionOrdering.Matching);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Generated_SyncWithCancellationToken_UsesRunSyncWithToken(CancellationToken CancellationToken)
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
            await Assert.That(generated).Contains("FungeRuntime.RunSync(");
            await Assert.That(generated).Contains("cancellationToken");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generated_Runtime_EmitsOnlyRequiredFacades_ForIntReturn(CancellationToken CancellationToken)
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
            languageVersion: LanguageVersion.CSharp9,
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var runtime = comp.SyntaxTrees
                .Select(static t => t.ToString())
                .Single(static text => text.Contains("internal static class FungeRuntime", StringComparison.Ordinal));

            await Assert.That(runtime)
                .Contains("internal static int RunSync(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static global::System.Threading.Tasks.Task RunTask(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static global::System.Threading.Tasks.ValueTask<string> RunValueTaskString(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static async global::System.Collections.Generic.IAsyncEnumerable<byte> RunAsyncEnumerable(", StringComparison.Ordinal);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Generated_Runtime_EmitsOnlyRequiredFacades_ForValueTaskString(CancellationToken CancellationToken)
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
                .Single(static text => text.Contains("file static class FungeRuntime", StringComparison.Ordinal));

            await Assert.That(runtime)
                .Contains("internal static global::System.Threading.Tasks.ValueTask<string> RunValueTaskString(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static int RunSync(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static global::System.Threading.Tasks.Task<int> RunTaskInt(", StringComparison.Ordinal)
                .And.DoesNotContain("internal static global::System.Collections.Generic.IEnumerable<byte> RunEnumerable(", StringComparison.Ordinal);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SyncCancellationToken_CancelsInfiniteLoop(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run")!;
            Assert.NotNull(m);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var ex = Assert.Throws<TargetInvocationException>(() => m.Invoke(null, [cts.Token]));
            Assert.NotNull(ex?.InnerException);
            await Assert.That(ex!.InnerException).IsTypeOf<OperationCanceledException>();
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void Generator_PreCanceledToken_ThrowsOperationCanceledException(CancellationToken CancellationToken)
    {
        const string source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod(InlineSource = "@")]
                public static partial int Run();
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        var driver = CSharpGeneratorDriver.Create(
            generators: [new MethodGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true))
            .WithUpdatedParseOptions(parseOptions);

        var compilation = baseCompilation
            .WithAssemblyName($"generatortest_{Interlocked.Increment(ref AssemblySequence)}")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions, path: "input.cs", encoding: Encoding.UTF8, cancellationToken: CancellationToken));

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var ex = Assert.Throws<OperationCanceledException>(() =>
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cts.Token));
        Assert.NotNull(ex);
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public void Generator_CancellationRequestedDuringAdditionalTextRead_ThrowsOperationCanceledException(CancellationToken CancellationToken)
    {
        const string source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                [GenerateFungeMethod("cancel-me.b98")]
                public static partial int Run();
            }
            """;

        var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp11);
        using var cts = new CancellationTokenSource();
        var additionalText = new CancellingAdditionalText(
            "cancel-me.b98",
            "@",
            onGetText: cts.Cancel);

        var driver = CSharpGeneratorDriver.Create(
            generators: [new MethodGenerator().AsSourceGenerator()],
            additionalTexts: [additionalText],
            driverOptions: new GeneratorDriverOptions(default, trackIncrementalGeneratorSteps: true))
            .WithUpdatedParseOptions(parseOptions);

        var compilation = baseCompilation
            .WithAssemblyName($"generatortest_{Interlocked.Increment(ref AssemblySequence)}")
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source, parseOptions, path: "input.cs", encoding: Encoding.UTF8, cancellationToken: CancellationToken));

        var ex = Assert.Throws<OperationCanceledException>(() =>
            driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _, cts.Token));
        Assert.NotNull(ex);
    }

    [Test]
    public async Task Diagnostic_SourceFileNotFound_FG0004(CancellationToken CancellationToken)
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
            await Assert.That(diag).Contains(d => d.Id == "FG0004").Because("Expected FG0004");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Diagnostic_DuplicateInputParameter_FG0006(CancellationToken CancellationToken)
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
            await Assert.That(diag).Contains(d => d.Id == "FG0006").Because("Expected FG0006");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task Diagnostic_ReturnOutputConflict_FG0007(CancellationToken CancellationToken)
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
            await Assert.That(diag).Contains(d => d.Id == "FG0007").Because("Expected FG0007");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.RuntimeOutputTimeout)]
#if NET48
    [NotInParallel(Constant.Net48NotInParallel)]
#endif
    public async Task Runtime_Int_TextWriter_ReturnsExitCodeAndWritesOutput(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.NotNull(m);
                using var output = new StringWriter();
                var result = (int)m.Invoke(null, [output, CancellationToken])!;
                await Assert.That(result).IsEqualTo(5);
                await Assert.That(output.ToString()).IsEqualTo("30 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.RuntimeOutputTimeout)]
#if NET48
    [NotInParallel(Constant.Net48NotInParallel)]
#endif

    public async Task Runtime_Int_PipeWriter_ReturnsExitCodeAndWritesOutput(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.NotNull(m);
            var pipe = new Pipe();
            var result = (int)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            await Assert.That(result).IsEqualTo(5);
            await Assert.That(await ReadPipeOutputAsync(pipe)).IsEqualTo("30 ");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.RuntimeOutputTimeout)]
#if NET48
    [NotInParallel(Constant.Net48NotInParallel)]
#endif

    public async Task Runtime_TaskInt_PipeWriter_ReturnsExitCodeAndWritesOutput(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.NotNull(m);
            var pipe = new Pipe();
            var result = await (Task<int>)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            await Assert.That(result).IsEqualTo(5);
            await Assert.That(await ReadPipeOutputAsync(pipe)).IsEqualTo("30 ");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.RuntimeOutputTimeout)]
#if NET48
    [NotInParallel(Constant.Net48NotInParallel)]
#endif

    public async Task Runtime_ValueTaskInt_PipeWriter_ReturnsExitCodeAndWritesOutput(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")!;
            var m = t.GetMethod("Run");
            Assert.NotNull(m);
            var pipe = new Pipe();
            var result = await (ValueTask<int>)m.Invoke(null, [pipe.Writer, CancellationToken])!;
            await Assert.That(result).IsEqualTo(5);
            await Assert.That(await ReadPipeOutputAsync(pipe)).IsEqualTo("30 ");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SelfModifiedOutputWithoutOutputInterface_Throws(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")
                ?? asm.GetType("TestClass");
            Assert.NotNull(t, "Failed to find generated type TestProject.TestClass.");
            var m = t.GetMethod("Run");
            Assert.NotNull(m, "Failed to find generated method Run.");

            var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, [CancellationToken]));
            Assert.NotNull(ex.InnerException);
            await Assert.That(ex.InnerException).IsTypeOf<InvalidOperationException>(); ;
            await Assert.That(ex.InnerException).HasMessageContaining("without an output interface");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SelfModifiedInputWithoutInputInterface_Throws(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            var t = asm.GetType("TestProject.TestClass")
                ?? asm.GetType("TestClass");
            Assert.NotNull(t, "Failed to find generated type TestProject.TestClass.");
            var m = t.GetMethod("Run");
            Assert.NotNull(m, "Failed to find generated method Run.");

            var ex = Assert.Throws<TargetInvocationException>(() => m!.Invoke(null, [CancellationToken]));
            Assert.NotNull(ex);
            Assert.NotNull(ex.InnerException);
            await Assert.That(ex.InnerException).IsTypeOf<InvalidOperationException>();
            await Assert.That(ex.InnerException).HasMessageContaining("without an input interface");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // InlineSource with multiple lines
    // -----------------------------------------------------------------------

    [Test]
    public async Task InlineSource_RawStringWithInnerQuotes_InspectGenerated(CancellationToken CancellationToken)
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
            await Assert.That(generated).Contains("__cells[(0, 0, 0)] = 64;");

            // Output all generated syntax trees for inspection
            LogWriteLine("=== Generated Syntax Trees ===");
            foreach (var tree in comp.SyntaxTrees)
            {
                LogWriteLine($"\n--- {tree.FilePath} ---");
                LogWriteLine(tree.GetText(CancellationToken).ToString());
            }
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task InlineSource_MultiLine_BasicProgram(CancellationToken CancellationToken)
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
            await Assert.That(generated).Contains("__cells[(0, 0, 0)] = 62;"); // '>'
            await Assert.That(generated).Contains("__cells[(1, 0, 0)] = 118;"); // 'v'
            await Assert.That(generated).Contains("__cells[(0, 1, 0)] = 94;"); // '^'
            await Assert.That(generated).Contains("__cells[(1, 1, 0)] = 64;"); // '@'
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task InlineSource_WithEscapedNewlines(CancellationToken CancellationToken)
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
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }
    [Test]
    [Timeout(Constant.Timeout)]
    public async Task Runtime_SystemInfo_ReportsCustomArgs(CancellationToken CancellationToken)
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

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run");
                Assert.NotNull(m);
                // Verify we can call it with the new parameters
                var result = (int?)m.Invoke(null, [new[] { "test" }, new[] { "VAR=VAL" }, CancellationToken]);
                await Assert.That(result).IsEqualTo(0);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    // -----------------------------------------------------------------------
    // FingerprintsProvider tests
    // -----------------------------------------------------------------------

    [Test]
    public async Task FingerprintsProvider_Property_GeneratesArgument(CancellationToken CancellationToken)
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
            await Assert.That(allGenerated).Contains("fingerprints:");
            await Assert.That(allGenerated).Contains("this.MyFingerprints");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_Method_GeneratesArgument(CancellationToken CancellationToken)
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
            await Assert.That(allGenerated).Contains("fingerprints:");
            await Assert.That(allGenerated).Contains("this.GetFingerprints()");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_StaticMethod_GeneratesQualifiedExpression(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System.Collections.Generic;f [GenerateFungeMethod(InlineSource = "\"TSEP\"4(A@", FingerprintsProvider = "GetFingerprints")]
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
            await Assert.That(allGenerated).Contains("fingerprints:");
            // Static member reference is fully qualified with global:: prefix
            await Assert.That(allGenerated).Contains("global::TestProject.TestClass.GetFingerprints()");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_InvalidMember_EmitsFG0012(CancellationToken CancellationToken)
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
            await Assert.That(diag).Contains(d => d.Id == "FG0012").Because("Expected FG0012 for unknown FingerprintsProvider member");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_InvalidMethodReturnType_EmitsFG0012(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                public static int GetFingerprints() => 42;

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "GetFingerprints")]
                public static partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            await Assert.That(diag).Contains(d => d.Id == "FG0012").Because("Expected FG0012 for invalid FingerprintsProvider return type");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_InvalidPropertyType_EmitsFG0012(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            namespace TestProject;
            partial class TestClass
            {
                public int Fingerprints => 42;

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "Fingerprints")]
                public partial void Run();
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            await Assert.That(diag).Contains(d => d.Id == "FG0012").Because("Expected FG0012 for invalid FingerprintsProvider property type");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_EnablesFingerprintSupportRuntime(CancellationToken CancellationToken)
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
            await Assert.That(runtime).Contains("RuntimeFungeExecutionContext");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    public async Task FingerprintsProvider_Runtime_GatesOptionalCapabilityInterfaces_WhenAbstractionsTypesAreMissing(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;

            namespace Esolang.Funge
            {

                public interface IFingerprint
                {
                    int Handprint { get; }
                    IReadOnlyDictionary<char, Func<IFungeExecutionContext, System.Threading.Tasks.ValueTask>> Instructions { get; }
                }

                public interface IFungeExecutionContext
                {
                    void Push(int value);
                    int Pop();
                    int Peek();
                    void Reflect();
                }
            }

            namespace TestProject;

            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints() => Array.Empty<IFingerprint>();

                [GenerateFungeMethod(InlineSource = "@", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);
            }
            """;
        RunGeneratorsAndUpdateCompilation(
            source,
            out var comp,
            out var diag,
            includeFungeAbstractionsReference: false,
            cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var runtime = string.Join("\n", comp.SyntaxTrees.Select(static t => t.ToString()));
            await Assert.That(runtime).Contains("global::Esolang.Funge.IFungeExecutionContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeInstructionPointerContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeInstructionPointerLifecycle");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeStackContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeInputContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeOutputContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeVectorContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeSpaceContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeStorageOffsetContext");
            await Assert.That(runtime).DoesNotContain("global::Esolang.Funge.IFungeRandomContext");
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_DispatchesInstruction(CancellationToken CancellationToken)
    {
        // Integration test: FingerprintsProvider wires up a real fingerprint.
        // PEST fingerprint with 'A' → pushes 42, program outputs it as integer.
        // Program: "TSEP"4(A.@ (load PEST, run A which pushes 42, output, stop)
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
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
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>> { ['A'] = ctx => { ctx.Push(42); return default; } };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(
                [.. diag.Where(v => v.Severity != DiagnosticSeverity.Hidden)], comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("42 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_VectorAndSpaceCapabilities_UseGeneratedRuntimeCapabilities(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
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
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = static ctx =>
                            {
                                if (ctx is not IFungeVectorContext vector || ctx is not IFungeSpaceContext space)
                                {
                                    ctx.Reflect();
                                    return default;
                                }

                                var (x, y, z) = vector.PopVector();
                                space.SetCell(x + 2, y, z, 42);
                                vector.PushVector(x, y, z);
                                ctx.Push(space.GetCell(x + 2, y, z));
                                return default;
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("42 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_InstructionPointerCapability_UsesGeneratedRuntimeCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new IpFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(A.@", FingerprintsProvider = "GetFingerprints")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);

                sealed class IpFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = static ctx =>
                            {
                                if (ctx is not IFungeInstructionPointerContext instructionPointer)
                                {
                                    ctx.Reflect();
                                    return default;
                                }

                                ctx.Push(instructionPointer.InstructionPointerId);
                                return default;
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("0 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_StackCapability_UsesGeneratedRuntimeCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new StackFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4($$12A.@", FingerprintsProvider = "GetFingerprints")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);

                sealed class StackFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = static ctx =>
                            {
                                if (ctx is not IFungeStackContext stack)
                                {
                                    ctx.Reflect();
                                    return default;
                                }

                                ctx.Push(stack.StackDepth);
                                return default;
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("2 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_RandomCapability_UsesGeneratedRuntimeCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new RandomFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(A.@", FingerprintsProvider = "GetFingerprints")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);

                sealed class RandomFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = static ctx =>
                            {
                                if (ctx is not IFungeRandomContext random)
                                {
                                    ctx.Reflect();
                                    return default;
                                }

                                random.Reseed(123u);
                                ctx.Push(random.NextUInt32(1) == 0 ? 1 : 0);
                                return default;
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("1 ");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_OutputCapability_UsesGeneratedOutputCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new OutputFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(A@", FingerprintsProvider = "GetFingerprints")]
                public static partial string Run(System.Threading.CancellationToken cancellationToken);

                sealed class OutputFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, System.Threading.Tasks.ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, System.Threading.Tasks.ValueTask>>
                        {
                            ['A'] = async ctx =>
                            {
                                if (ctx is not IFungeOutputContext output)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                await output.WriteStringAsync("OK");
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (string?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo("OK");
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_OutputCapability_ReflectsWithoutGeneratedOutputCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new OutputFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(#@A1q", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);

                sealed class OutputFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = async ctx =>
                            {
                                if (ctx is not IFungeOutputContext output)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                await output.WriteStringAsync("OK");
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(0);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_InputCapability_UsesGeneratedInputCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new InputFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(Aq", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(string input, System.Threading.CancellationToken cancellationToken);

                sealed class InputFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = async ctx =>
                            {
                                if (ctx is not IFungeInputContext input)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                var line = await input.ReadLineAsync();
                                if (line is null)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                ctx.Push(int.Parse(line, CultureInfo.InvariantCulture));
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, ["5", CancellationToken]);
                await Assert.That(result).IsEqualTo(5);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_InputCapability_ReflectsWithoutGeneratedInputCapability(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Globalization;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { new InputFingerprint() };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(#@A1q", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);

                sealed class InputFingerprint : IFingerprint
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>
                        {
                            ['A'] = async ctx =>
                            {
                                if (ctx is not IFungeInputContext input)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                var line = await input.ReadLineAsync();
                                if (line is null)
                                {
                                    ctx.Reflect();
                                    return;
                                }

                                ctx.Push(int.Parse(line, CultureInfo.InvariantCulture));
                            },
                        };
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(0);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
            throw;
        }
    }

    [Test]
    [Timeout(Constant.Timeout)]
    public async Task FingerprintsProvider_Functional_InstructionPointerLifecycle_NotifiesCloneAndTermination(CancellationToken CancellationToken)
    {
        var source = """
            using Esolang.Funge;
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestProject;
            partial class TestClass
            {
                static LifecycleFingerprint Fingerprint { get; } = new();
                public static IEnumerable<IFingerprint> GetFingerprints()
                    => new IFingerprint[] { Fingerprint };

                [GenerateFungeMethod(InlineSource = "\"TSEP\"4(>tq", FingerprintsProvider = "GetFingerprints")]
                public static partial int Run(System.Threading.CancellationToken cancellationToken);

                sealed class LifecycleFingerprint : IFingerprint, IFungeInstructionPointerLifecycle
                {
                    public int Handprint => FingerprintHandprint.Compute("PEST");
                    public List<string> Events { get; } = new();
                    public IReadOnlyDictionary<char, Func<IFungeExecutionContext, ValueTask>> Instructions { get; }
                        = new Dictionary<char, Func<IFungeExecutionContext, ValueTask>>();

                    public void OnInstructionPointerCloned(int parentInstructionPointerId, int childInstructionPointerId)
                        => Events.Add($"clone:{parentInstructionPointerId}->{childInstructionPointerId}");

                    public void OnInstructionPointerTerminated(int instructionPointerId)
                        => Events.Add($"term:{instructionPointerId}");
                }
            }
            """;
        RunGeneratorsAndUpdateCompilation(source, out var comp, out var diag, cancellationToken: CancellationToken);
        try
        {
            AssertNoErrors(diag, comp);

            var asm = await EmitAsync(comp, CancellationToken);
            await Task.Factory.StartNew(async () =>
            {
                var t = asm.GetType("TestProject.TestClass")!;
                var m = t.GetMethod("Run")!;
                var result = (int?)m.Invoke(null, [CancellationToken]);
                await Assert.That(result).IsEqualTo(1);

                var fingerprint = t.GetProperty("Fingerprint", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                var events = (IEnumerable<string>)fingerprint.GetType().GetProperty("Events")!.GetValue(fingerprint)!;
                await Assert.That(events).IsEquivalentTo((string[])["clone:0->1", "term:0", "term:1"], CollectionOrdering.Any);
            }, CancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }
        catch (Exception e) when (e is AssertionException or TargetInvocationException)
        {
            LogDiagnostics(diag, comp, CancellationToken);
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

file sealed class CancellingAdditionalText(string path, string content, Action onGetText) : AdditionalText
{
    public override string Path { get; } = path;

    public override SourceText? GetText(CancellationToken cancellationToken = default)
    {
        onGetText();
        cancellationToken.ThrowIfCancellationRequested();
        return SourceText.From(content, Encoding.UTF8);
    }
}


file static class Constant
{
#if NET48
    public const int Timeout = 1000 * 120;
#else
    public const int Timeout = 1000 * 30;
#endif
    public const int RuntimeOutputTimeout = Timeout;
#if NET48
    public const string Net48NotInParallel = "Esolang.Funge.Generator.Tests." + nameof(Net48NotInParallel);
#endif
}
