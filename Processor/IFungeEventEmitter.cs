using Esolang.Processor;

namespace Esolang.Funge.Processor;

/// <summary>
/// Represents an emitter for Funge events.
/// </summary>
interface IFungeEventEmitter
{
    Task Emit(IOEvent ioEvent);
}
