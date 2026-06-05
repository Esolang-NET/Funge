using Esolang.Funge.Fingerprints.Arry;
using Esolang.Funge.Fingerprints.Base;
using Esolang.Funge.Fingerprints.Bool;
using Esolang.Funge.Fingerprints.Date;
using Esolang.Funge.Fingerprints.File;
using Esolang.Funge.Fingerprints.Fixp;
using Esolang.Funge.Fingerprints.Indv;
using Esolang.Funge.Fingerprints.Modu;
using Esolang.Funge.Fingerprints.Rand;
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
        yield return Arry;
        yield return Date;
        yield return Bool;
        yield return File;
        yield return Fixp;
        yield return Indv;
        yield return Modu;
        yield return Rand;
        yield return Roma;
        yield return Strn;
        yield return Time;
        yield return Base;
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
    /// Enable the ARRY fingerprint (0x41525259): array instructions A B C D E F G.
    /// </summary>
    readonly Pair Arry = new(new(name: "--fingerprint-arry")
    {
        Description = "Enable the ARRY fingerprint (0x41525259): array instructions A B C D E F G.",
    }, _ => new ArrayFingerprint());

    /// <summary>
    /// Enable the BASE fingerprint (0x42415345): base conversion instructions B H I N O.
    /// </summary>
    readonly Pair Base = new(new(name: "--fingerprint-base")
    {
        Description = "Enable the BASE fingerprint (0x42415345): base conversion instructions B H I N O",
    }, _ => new BaseFingerprint());

    /// <summary>
    /// Enable the BOOL fingerprint (0x424F4F4C): logic instructions A N O X.
    /// </summary>
    readonly Pair Bool = new(new(name: "--fingerprint-bool")
    {
        Description = "Enable the BOOL fingerprint (0x424F4F4C): logic instructions A N O X.",
    }, _ => new BoolFingerprint());

    /// <summary>
    /// Enable the DATE fingerprint (0x44415445): Gregorian calendar instructions A C D J T W Y.
    /// </summary>
    readonly Pair Date = new(new(name: "--fingerprint-date")
    {
        Description = "Enable the DATE fingerprint (0x44415445): Gregorian calendar instructions A C D J T W Y.",
    }, _ => new DateFingerprint());

    /// <summary>
    /// Enable the FILE fingerprint (0x46494C45): file I/O instructions C D G M O P R S W.
    /// </summary>
    readonly Pair File = new(new(name: "--fingerprint-file")
    {
        Description = "Enable the FILE fingerprint (0x46494C45): file I/O instructions C D G M O P R S W.",
    }, _ => new FileFingerprint());

    /// <summary>
    /// Enable the FIXP fingerprint (0x46495850): fixed-point instructions A B C D I J N O P Q R S T U V X.
    /// </summary>
    readonly Pair Fixp = new(new(name: "--fingerprint-fixp")
    {
        Description = "Enable the FIXP fingerprint (0x46495850): fixed-point instructions A B C D I J N O P Q R S T U V X.",
    }, _ => new FixedPointFingerprint());

    /// <summary>
    /// Enable the INDV fingerprint (0x494E4456): indirect vector instructions G P V W.
    /// </summary>
    readonly Pair Indv = new(new(name: "--fingerprint-indv")
    {
        Description = "Enable the INDV fingerprint (0x494E4456): indirect vector instructions G P V W.",
    }, _ => new IndirectVectorFingerprint());

    /// <summary>
    /// Enable the RAND fingerprint (0x52414E44): random instructions I M R S T.
    /// </summary>
    readonly Pair Rand = new(new(name: "--fingerprint-rand")
    {
        Description = "Enable the RAND fingerprint (0x52414E44): random instructions I M R S T.",
    }, _ => new RandomFingerprint());

    /// <summary>
    /// Enable the MODU fingerprint (0x4D4F4455): modulo instructions M R U.
    /// </summary>
    readonly Pair Modu = new(new(name: "--fingerprint-modu")
    {
        Description = "Enable the MODU fingerprint (0x4D4F4455): modulo instructions M R U.",
    }, _ => new ModuloFingerprint());

    /// <summary>
    /// Enable the ROMA fingerprint (0x524F4D41): Roma instructions D G H M S T Y.
    /// </summary>
    readonly Pair Roma = new(new(name: "--fingerprint-roma")
    {
        Description = "Enable the ROMA fingerprint (0x524F4D41): Roma instructions D G H M S T Y.",
    }, _ => new RomanFingerprint());

    /// <summary>
    /// Enable the STRN fingerprint (0x5354524E): string instructions A C L N R S.
    /// </summary>
    readonly Pair Strn = new(new(name: "--fingerprint-strn")
    {
        Description = "Enable the STRN fingerprint (0x5354524E): string instructions A C L N R S.",
    }, _ => new StringFingerprint());

    /// <summary>
    /// Enable the TIME fingerprint (0x54494D45): time instructions A N O X.
    /// </summary>
    readonly Pair Time = new(new(name: "--fingerprint-time")
    {
        Description = "Enable the TIME fingerprint (0x54494D45): time instructions A N O X.",
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
