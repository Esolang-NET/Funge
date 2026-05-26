using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Esolang.Funge.Generator.Tests;

[TestClass]
public class PartialMethodConstraintTests(TestContext TestContext)
{

#pragma warning disable MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    CancellationToken Cancellationtoken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054 // TestContext.CancellationTokenSource.Token の代わりに TestContext.CancellationToken を使用する
    [TestMethod]
    public void Generator_NonPartialMethod_ReportsError()
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
            [CSharpSyntaxTree.ParseText(source, cancellationToken: Cancellationtoken)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        var generator = new MethodGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics, Cancellationtoken);

        Assert.Contains(d => d.Id == "FG0011", diagnostics, "Expected diagnostic FG0011 (Method must be partial)");
    }
}
