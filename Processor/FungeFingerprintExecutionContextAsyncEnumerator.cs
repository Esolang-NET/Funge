using Esolang.Processor;
using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;

namespace Esolang.Funge.Processor;

/// <summary>
/// Implements the async enumerator returned by <see cref="FungeFingerprintExecutionContext.GetAsyncEnumerator"/>.
/// </summary>
/// <param name="function"></param>
/// <param name="ioEventRequestWaiters"></param>
/// <param name="ioEventResponseWaiters"></param>
/// <param name="context"></param>
/// <param name="cancellationToken"></param>
class FungeFingerprintExecutionContextAsyncEnumerator(FingerprintInstruction function,
    List<TaskCompletionSource<IOEvent>> ioEventRequestWaiters,
    List<TaskCompletionSource> ioEventResponseWaiters,
    FungeFingerprintExecutionContext context,
    CancellationToken cancellationToken) : IDisposable
{
    /// <summary>
    /// The state of the async enumerator.
    /// </summary>
    State state = default;

    /// <summary>
    /// The current index in the list of IO event request waiters.
    /// </summary>
    int i = 0;

    /// <summary>
    /// The cancellation token used to cancel the enumeration.
    /// </summary>
    CancellationTokenRegistration? cancellationTokenRegistration;

    /// <summary>
    /// The cancellation token used to cancel the enumeration.
    /// </summary>
    IOEvent current = default!;

    /// <summary>
    /// The task representing the execution of the fingerprint function.
    /// </summary>
    Task functionTask = default!;

    /// <summary>
    /// The task representing the cancellation of the enumeration.
    /// </summary>
    Task cancelTask = default!;

    /// <summary>
    /// The task completion source for the current IO event request.
    /// </summary>
    TaskCompletionSource<IOEvent> request = default!;

    /// <summary>
    /// The task completion source for the current IO event response.
    /// </summary>
    TaskCompletionSource response = default!;

    /// <summary>
    /// Gets the element in the collection at the current position of the enumerator.
    /// </summary>
    /// <returns>The element in the collection at the current position of the enumerator.</returns>
    public IOEvent Current => current;

    /// <summary>
    /// The state of the async enumerator.
    /// </summary>
    enum State : int
    {
        /// <summary>
        /// The initial state of the async enumerator.
        /// </summary>
        Initial = default,

        /// <summary>
        /// The async enumerator is waiting for an IO event.
        /// </summary>
        WaitingForEvent = 1,

        /// <summary>
        /// The async enumerator has finished enumeration.
        /// </summary>
        Finished = -1,
    }

    /// <summary>
    /// Advances the enumerator asynchronously to the next element of the collection.
    /// </summary>
    /// <returns>A <see cref="ValueTask{TResult}"/> that will complete with a result of true if the enumerator was successfully advanced to the next element, or false if the enumerator has passed the end of the collection.</returns>
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

                lock (ioEventRequestWaiters)
                {
                    request = ioEventRequestWaiters[i++];
                }

                await Task.WhenAny(functionTask, request.Task, cancelTask);

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

                if (cancelTask.IsCompletedSuccessfully)
                {
                    state = State.Finished;
                    return false;
                }

                if (ioEventRequestWaiters.Count <= i)
                {
                    if (functionTask.IsCompleted)
                    {
                        state = State.Finished;
                        return false;
                    }
                    await Task.Yield();
                    return await MoveNextAsync();
                }

                lock (ioEventRequestWaiters)
                {
                    request = ioEventRequestWaiters[i++];
                }

                await Task.WhenAny(functionTask, request.Task, cancelTask);

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

    public void Dispose()
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
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    static (Task, CancellationTokenRegistration) AsTaskWithRegistration(CancellationToken cancellationToken)
    {
        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        var registration = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false);
        return (tcs.Task, registration);
    }

    public override string ToString() => nameof(FungeFingerprintExecutionContextAsyncEnumerator) + $"{{State={state}, i={i}}}";
}
