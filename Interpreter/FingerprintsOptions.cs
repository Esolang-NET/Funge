using Esolang.Funge.Fingerprints.File;
using Esolang.Funge.Fingerprints.Modu;
using Esolang.Funge.Fingerprints.Roma;
using Esolang.Funge.Fingerprints.Strn;
using Esolang.Funge.Fingerprints.Time;
using System.Collections;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;

namespace Esolang.Funge.Interpreter;

/// <summary>
/// Represents a collection of command-line options for enabling various fingerprints in the Funge-98 interpreter. Each option corresponds to a specific fingerprint that can be enabled, allowing users to customize the behavior of the interpreter based on their needs.
/// </summary>
class FingerprintsOptions : IEnumerable<IOptionAndMakeFingerprintPair>
{
    public IEnumerable<Option> Options => this.SelectMany(pair => pair);
    public IEnumerable<IFingerprint>? GetValues(ParseResult parseResult)
    {
        var values = GetValues();
        if (values.Any())
            return values;
        return null;
        IEnumerable<IFingerprint> GetValues()
        {
            foreach (var pair in this)
                if (pair.TryGetValue(parseResult, out var fingerprint))
                    yield return fingerprint;
            yield break;
        }
    }

    public IEnumerator<IOptionAndMakeFingerprintPair> GetEnumerator()
    {
        yield return Null;
        yield return File;
        yield return Modu;
        yield return Roma;
        yield return Strn;
        yield return Time;

    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Enable the NULL fingerprint (0x4E554C4C): all 26 instructions reflect.
    /// </summary>
    readonly Pair Null = new(new(name: "--fingerprint-null")
    {
        Description = "Enable the NULL fingerprint (0x4E554C4C): all 26 instructions reflect.",
    }, _ => NullFingerprint.Instance);

    /// <summary>
    /// Enable the FILE fingerprint (0x46494C45): file I/O instructions C D G M O P R S W.
    /// </summary>
    readonly Pair File = new(new(name: "--fingerprint-file")
    {
        Description = "Enable the FILE fingerprint (0x46494C45): file I/O instructions C D G M O P R S W.",
    }, _ => new FileFingerprint());

    /// <summary>
    /// Enable the MODU fingerprint (0x4D4F4455): modulo instructions.
    /// </summary>
    readonly Pair Modu = new(new(name: "--fingerprint-modu")
    {
        Description = "Enable the MODU fingerprint (0x4D4F4455): modulo instructions.",
    }, _ => new ModuloFingerprint());

    /// <summary>
    /// Enable the ROMA fingerprint (0x524F4D41): Roma instructions.
    /// </summary>
    readonly Pair Roma = new(new(name: "--fingerprint-roma")
    {
        Description = "Enable the ROMA fingerprint (0x524F4D41): Roma instructions.",
    }, _ => new RomanFingerprint());

    /// <summary>
    /// Enable the STRN fingerprint (0x5354524E): string instructions.
    /// </summary>
    readonly Pair Strn = new(new(name: "--fingerprint-strn")
    {
        Description = "Enable the STRN fingerprint (0x5354524E): string instructions.",
    }, _ => new StringFingerprint());

    /// <summary>
    /// Enable the TIME fingerprint (0x54494D45): time instructions.
    /// </summary>
    readonly Pair Time = new(new(name: "--fingerprint-time")
    {
        Description = "Enable the TIME fingerprint (0x54494D45): time instructions.",
    }, _ => new TimeFingerprint());

    /// <summary>
    /// Defines a pair of an <see cref="Option"/> and a factory for creating an <see cref="IFingerprint"/> based on the parsed command-line arguments.
    /// </summary>
    /// <param name="Option"></param>
    /// <param name="Factory"></param>
    record class Pair(Option<bool> Option, Func<ParseResult, IFingerprint?> Factory) : IOptionAndMakeFingerprintPair
    {
        public virtual IEnumerator<Option> GetEnumerator()
        {
            yield return Option;
        }

        public virtual bool TryGetValue(ParseResult parseResult, [NotNullWhen(true)] out IFingerprint? fingerprint)
        {
            if (parseResult.GetValue(Option) && Factory(parseResult) is IFingerprint fp)
            {
                fingerprint = fp;
                return true;
            }
            fingerprint = null;
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

}
