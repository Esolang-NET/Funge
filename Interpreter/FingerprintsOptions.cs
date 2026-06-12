using Esolang.Funge.Fingerprints.Arry;
using Esolang.Funge.Fingerprints.Base;
using Esolang.Funge.Fingerprints.Bool;
using Esolang.Funge.Fingerprints.Cpli;
using Esolang.Funge.Fingerprints.Date;
using Esolang.Funge.Fingerprints.Dirf;
using Esolang.Funge.Fingerprints.Evar;
using Esolang.Funge.Fingerprints.File;
using Esolang.Funge.Fingerprints.Fixp;
using Esolang.Funge.Fingerprints.Fpdp;
using Esolang.Funge.Fingerprints.Fprt;
using Esolang.Funge.Fingerprints.Fpsp;
using Esolang.Funge.Fingerprints.Hrti;
using Esolang.Funge.Fingerprints.Ical;
using Esolang.Funge.Fingerprints.Indv;
using Esolang.Funge.Fingerprints.Jstr;
using Esolang.Funge.Fingerprints.Modu;
using Esolang.Funge.Fingerprints.Orth;
using Esolang.Funge.Fingerprints.Rand;
using Esolang.Funge.Fingerprints.Refc;
using Esolang.Funge.Fingerprints.Roma;
using Esolang.Funge.Fingerprints.Sets;
using Esolang.Funge.Fingerprints.Strn;
using Esolang.Funge.Fingerprints.Term;
using Esolang.Funge.Fingerprints.ThreeDsp;
using Esolang.Funge.Fingerprints.Time;
using Esolang.Funge.Fingerprints.Toys;
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
        yield return Cpli;
        yield return Dirf;
        yield return Evar;
        yield return File;
        yield return Fixp;
        yield return Fpdp;
        yield return Fprt;
        yield return Fpsp;
        yield return Hrti;
        yield return Ical;
        yield return Indv;
        yield return Jstr;
        yield return Modu;
        yield return Orth;
        yield return Rand;
        yield return Refc;
        yield return Roma;
        yield return Sets;
        yield return Strn;
        yield return Term;
        yield return ThreeDsp;
        yield return Time;
        yield return Toys;
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
    /// Enable the CPLI fingerprint (0x43504C49): complex integer arithmetic A D M O S V.
    /// </summary>
    readonly Pair Cpli = new(new(name: "--fingerprint-cpli")
    {
        Description = "Enable the CPLI fingerprint (0x43504C49): complex integer arithmetic A D M O S V.",
    }, _ => new ComplexIntegerFingerprint());

    /// <summary>
    /// Enable the DIRF fingerprint (0x44495246): directory functions C M R.
    /// </summary>
    readonly Pair Dirf = new(new(name: "--fingerprint-dirf")
    {
        Description = "Enable the DIRF fingerprint (0x44495246): directory functions C M R.",
    }, _ => new DirectoryFingerprint());

    /// <summary>
    /// Enable the EVAR fingerprint (0x45564152): environment variable instructions G N P V.
    /// </summary>
    readonly Pair Evar = new(new(name: "--fingerprint-evar")
    {
        Description = "Enable the EVAR fingerprint (0x45564152): environment variable instructions G N P V.",
    }, _ => new EnvironmentVariablesFingerprint());

    /// <summary>
    /// Enable the FPDP fingerprint (0x46504450): double-precision floating point A B C D E F G H I K L M N P Q S T V X Y.
    /// </summary>
    readonly Pair Fpdp = new(new(name: "--fingerprint-fpdp")
    {
        Description = "Enable the FPDP fingerprint (0x46504450): double-precision floating point A B C D E F G H I K L M N P Q S T V X Y.",
    }, _ => new DoublePrecisionFloatFingerprint());

    /// <summary>
    /// Enable the FPRT fingerprint (0x46505254): formatted print D F I L S.
    /// </summary>
    readonly Pair Fprt = new(new(name: "--fingerprint-fprt")
    {
        Description = "Enable the FPRT fingerprint (0x46505254): formatted print D F I L S.",
    }, _ => new FormattedPrintFingerprint());

    /// <summary>
    /// Enable the FPSP fingerprint (0x46505350): single-precision floating point A B C D E F G H I K L M N P Q S T V X Y.
    /// </summary>
    readonly Pair Fpsp = new(new(name: "--fingerprint-fpsp")
    {
        Description = "Enable the FPSP fingerprint (0x46505350): single-precision floating point A B C D E F G H I K L M N P Q S T V X Y.",
    }, _ => new SinglePrecisionFloatFingerprint());

    /// <summary>
    /// Enable the HRTI fingerprint (0x48525449): high-resolution timer E G M S T.
    /// </summary>
    readonly Pair Hrti = new(new(name: "--fingerprint-hrti")
    {
        Description = "Enable the HRTI fingerprint (0x48525449): high-resolution timer E G M S T.",
    }, _ => new HighResTimerFingerprint());

    /// <summary>
    /// Enable the ICAL fingerprint (0x4943414C): intercal-like instructions A F I N O R S X.
    /// </summary>
    readonly Pair Ical = new(new(name: "--fingerprint-ical")
    {
        Description = "Enable the ICAL fingerprint (0x4943414C): intercal-like instructions A F I N O R S X.",
    }, _ => new IntercalFingerprint());

    /// <summary>
    /// Enable the JSTR fingerprint (0x4A535452): Jesse van Herk's string extensions G P.
    /// </summary>
    readonly Pair Jstr = new(new(name: "--fingerprint-jstr")
    {
        Description = "Enable the JSTR fingerprint (0x4A535452): Jesse van Herk's string extensions G P.",
    }, _ => new JstrFingerprint());

    /// <summary>
    /// Enable the ORTH fingerprint (0x4F525448): orthogonal easement A E G O P S V W X Y Z.
    /// </summary>
    readonly Pair Orth = new(new(name: "--fingerprint-orth")
    {
        Description = "Enable the ORTH fingerprint (0x4F525448): orthogonal easement A E G O P S V W X Y Z.",
    }, _ => new OrthogonalFingerprint());

    /// <summary>
    /// Enable the REFC fingerprint (0x52454643): referenced cells D R.
    /// </summary>
    readonly Pair Refc = new(new(name: "--fingerprint-refc")
    {
        Description = "Enable the REFC fingerprint (0x52454643): referenced cells D R.",
    }, _ => new ReferencedCellsFingerprint());

    /// <summary>
    /// Enable the SETS fingerprint (0x53455453): set operations A C D G I M P R S U W X Z.
    /// </summary>
    readonly Pair Sets = new(new(name: "--fingerprint-sets")
    {
        Description = "Enable the SETS fingerprint (0x53455453): set operations A C D G I M P R S U W X Z.",
    }, _ => new SetOperationsFingerprint());

    /// <summary>
    /// Enable the TERM fingerprint (0x5445524D): terminal extension C D G H L S U.
    /// </summary>
    readonly Pair Term = new(new(name: "--fingerprint-term")
    {
        Description = "Enable the TERM fingerprint (0x5445524D): terminal extension C D G H L S U.",
    }, _ => new TerminalFingerprint());

    /// <summary>
    /// Enable the 3DSP fingerprint (0x33445350): 3D space manipulation A B C D L M N P R S T U V X Y Z.
    /// </summary>
    readonly Pair ThreeDsp = new(new(name: "--fingerprint-3dsp")
    {
        Description = "Enable the 3DSP fingerprint (0x33445350): 3D space manipulation A B C D L M N P R S T U V X Y Z.",
    }, _ => new ThreeDeeSpaceFingerprint());

    /// <summary>
    /// Enable the TOYS fingerprint (0x544F5953): Funge-98 standard toys A B C D E F G H I J K L M N O P Q R S T U V W X Y Z.
    /// </summary>
    readonly Pair Toys = new(new(name: "--fingerprint-toys")
    {
        Description = "Enable the TOYS fingerprint (0x544F5953): Funge-98 standard toys A B C D E F G H I J K L M N O P Q R S T U V W X Y Z.",
    }, _ => new ToysFingerprint());

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
