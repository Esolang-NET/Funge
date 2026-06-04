namespace Esolang.Funge;

/// <summary>
/// Represents a single Funge-98 fingerprint instruction handler.
/// </summary>
/// <param name="context">The execution context for the current instruction pointer.</param>
public delegate void FingerprintInstruction(IFungeExecutionContext context);
