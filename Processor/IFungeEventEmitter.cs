using Esolang.Processor;

namespace Esolang.Funge.Processor;

interface IFungeEventEmitter
{
    Task Emit(IOEvent ioEvent);
}
