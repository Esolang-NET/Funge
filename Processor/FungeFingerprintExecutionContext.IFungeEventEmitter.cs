using Esolang.Processor;

namespace Esolang.Funge.Processor;

abstract partial class FungeFingerprintExecutionContext
    : IFungeEventEmitter
{
    Task IFungeEventEmitter.Emit(IOEvent ioEvent)
    {
        TaskCompletionSource<IOEvent> last;
        lock (ioEventRequestWaiters)
        {
            last = ioEventRequestWaiters[^1];
            ioEventRequestWaiters.Add(new());
        }
        last.TrySetResult(ioEvent);
        TaskCompletionSource source = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ioEventResponseWaiters.Add(source);
        return source.Task;
    }
}
