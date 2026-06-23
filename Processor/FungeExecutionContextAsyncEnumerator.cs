using Esolang.Processor;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

/// <summary>
/// Implements the async enumerator returned by <see cref="FungeExecutionContext.GetAsyncEnumerator"/>.
/// </summary>
/// <param name="function"></param>
/// <param name="ioEventRequestWaiters"></param>
/// <param name="ioEventResponseWaiters"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
class FungeExecutionContextAsyncEnumerator(FingerprintInstruction function,
    List<TaskCompletionSource<IOEvent>> ioEventRequestWaiters,
    List<TaskCompletionSource> ioEventResponseWaiters, FungeExecutionContext context, CancellationToken cancellationToken)
{
    State state = State.Initial;
    int i = 0;
    CancellationTokenRegistration? cancellationTokenRegistration;
    IOEvent current = default!;
    Task functionTask = default!;
    Task cancelTask = default!;
    TaskCompletionSource<IOEvent> request = default!;
    TaskCompletionSource response = default!;
    public IOEvent Current => current;
    enum State : int
    {
        Initial = 0,
        WaitingForEvent = 1,
        Finished = -1,
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        if (cancellationToken.IsCancellationRequested)
            state = State.Finished;
        switch (state)
        {
            default:
                // finished
                return false;
            case State.Initial:
                // initial
                var task = function(context);
                if (task.IsCompleted && ioEventRequestWaiters.Count <= 1)
                {
                    state = State.Finished;
                    return false;
                }
                (cancelTask, cancellationTokenRegistration) = AsTaskWithRegistration(cancellationToken);
                functionTask = task.AsTask();
                request = ioEventRequestWaiters[i++];
                await Task.WhenAny(functionTask, request.Task, cancelTask).ContinueWith(_ => { });
                if (cancellationToken.IsCancellationRequested)
                {
                    state = State.Finished;
                    return false;
                }
                if (request.Task.IsCompletedSuccessfully)
                {
                    response = ioEventResponseWaiters[i - 1];
                    current = await request.Task;
                    state = State.WaitingForEvent;
                    return true;
                }
                state = State.Finished;
                return false;
            case State.WaitingForEvent:
                // waiting for next event
                response.TrySetResult();
                if (cancelTask.IsCompletedSuccessfully || ioEventRequestWaiters.Count <= i)
                {
                    state = State.Finished;
                    return false;
                }
                request = ioEventRequestWaiters[i++];
                await Task.WhenAny(functionTask, request.Task, cancelTask).ContinueWith(_ => { });
                if (cancellationToken.IsCancellationRequested)
                {
                    state = State.Finished;
                    return false;
                }
                if (request.Task.IsCompletedSuccessfully)
                {
                    response = ioEventResponseWaiters[i - 1];
                    current = await request.Task;
                    state = State.WaitingForEvent;
                    return true;
                }
                state = State.Finished;
                return false;
        }
    }

    public ValueTask DisposeAsync()
    {
        cancellationTokenRegistration?.Dispose();
        cancellationTokenRegistration = null;
        if (cancellationToken.IsCancellationRequested)
        {
            foreach (var waiter in ioEventRequestWaiters)
                waiter.TrySetCanceled(cancellationToken);
            foreach (var waiter in ioEventResponseWaiters)
                waiter.TrySetCanceled(cancellationToken);
        }
        return ValueTask.CompletedTask;
    }

    static (Task, CancellationTokenRegistration) AsTaskWithRegistration(CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource();
        var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
        return (tcs.Task, registration);
    }

    public override string ToString() => nameof(FungeExecutionContextAsyncEnumerator) + $"{{State={state}, i={i}}}";
}
