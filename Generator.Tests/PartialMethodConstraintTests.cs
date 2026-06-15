using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Esolang.Funge.Generator.Tests;

public class PartialMethodConstraintTests
{

    [Test]
    public async Task Generator_NonPartialMethod_ReportsError(CancellationToken CancellationToken)
    {
        const string source = """
            namespace Demo;

            public class Sample
            {
                [Esolang.Funge.GenerateFungeMethod(InlineSource = ">@")]
                public void RunSync() { }
            }
            """;

        var compilation = CSharpCompilation.Create("Test",
            [CSharpSyntaxTree.ParseText(source, cancellationToken: CancellationToken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var generator = new MethodGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics, CancellationToken);

        await Assert.That(diagnostics).Contains(d => d.Id == "FG0011").Because("Expected diagnostic FG0011 (Method must be partial)");
    }
}
