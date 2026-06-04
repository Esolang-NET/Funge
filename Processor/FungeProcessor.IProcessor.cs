
using Esolang.Funge.Parser;
using Esolang.Processor;
using System.Diagnostics.CodeAnalysis;

namespace Esolang.Funge.Processor;

public partial class FungeProcessor : IProcessor<FungeSpace>
{
    /// <inheritdoc/>
    [ExcludeFromCodeCoverage]
    FungeSpace IProcessor<FungeSpace>.Program => _space;
}
