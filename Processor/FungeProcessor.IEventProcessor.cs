using Esolang.Processor;
using System.Runtime.CompilerServices;
using static Esolang.Processor.IOEvent;

namespace Esolang.Funge.Processor;

public sealed partial class FungeProcessor : IEventProcessor
{

    sealed class FungeState
    {
        public int ExitCode;
        public bool Quit;
        public bool SuppressAdvance;
    }

    /// <summary>
    /// Runs the Funge-98 program and returns the process exit code.
    /// The program starts with a single IP at (0,0) moving East.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel execution.</param>
    /// <returns>Exit code: 0 unless the program used <c>q</c>.</returns>
    public async IAsyncEnumerable<IOEvent> RunAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ips = new LinkedList<InstructionPointer>();
        ips.AddFirst(new InstructionPointer(_nextIpId++));
        var state = new FungeState();

        try
        {
            while (ips.Count > 0 && !state.Quit && !cancellationToken.IsCancellationRequested)
            {
                var node = ips.First;
                while (node is not null && !state.Quit && !cancellationToken.IsCancellationRequested)
                {
                    var nextNode = node.Next;
                    var ip = node.Value;

                    state.SuppressAdvance = false;
                    foreach (var ev in ExecuteInstruction(ip, ips, node, state))
                    {
                        yield return ev;
                    }

                    if (ip.IsStopped || state.Quit)
                    {
                        NotifyInstructionPointerTerminated(ip.Id);
                        ips.Remove(node);
                    }
                    else if (!state.SuppressAdvance)
                    {
                        ip.Position = _space.Advance(ip.Position, ip.Delta);
                    }

                    node = nextNode;
                }
            }
        }
        finally
        {
            while (ips.Count > 0)
            {
                NotifyInstructionPointerTerminated(ips.First!.Value.Id);
                ips.RemoveFirst();
            }
        }

        yield return new EndEvent(state.ExitCode);
    }
}
