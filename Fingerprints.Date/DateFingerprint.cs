using FingerprintInstruction = System.Func<Esolang.Funge.IFungeExecutionContext, System.Threading.Tasks.ValueTask>;
namespace Esolang.Funge.Fingerprints.Date;

/// <summary>
/// Provides the standard Funge-98 <c>DATE</c> fingerprint (handprint <c>0x44415445</c>).
/// </summary>
public sealed class DateFingerprint : IFingerprint
{
    /// <inheritdoc/>
    public int Handprint { get; } = FingerprintHandprint.Compute("DATE");

    /// <inheritdoc/>
    public IReadOnlyDictionary<char, FingerprintInstruction> Instructions { get; }

    /// <summary>Initializes a new instance of <see cref="DateFingerprint"/>.</summary>
    public DateFingerprint() =>
        Instructions = new FingerprintBuilder()
            .Add('A', AddDays)
            .Add('C', JulianDayToCalendarDate)
            .Add('D', DaysBetweenDates)
            .Add('J', CalendarDateToJulianDay)
            .Add('T', YearDayOfYearToDate)
            .Add('W', DayOfWeek)
            .Add('Y', DayOfYear)
            .BuildInstructions();

    static void AddDays(IFungeExecutionContext ctx)
    {
        var days = ctx.Pop();
        if (!TryPopDate(ctx, out var year, out var month, out var day))
            return;

        var julianDay = ToJulianDayNumber(year, month, day);
        var resultingJulianDay = (long)julianDay + days;
        if (!TryPushDateFromJulianDay(ctx, resultingJulianDay))
            ctx.Reflect();
    }

    static void JulianDayToCalendarDate(IFungeExecutionContext ctx)
    {
        var julianDay = ctx.Pop();
        if (!TryPushDateFromJulianDay(ctx, julianDay))
            ctx.Reflect();
    }

    static void DaysBetweenDates(IFungeExecutionContext ctx)
    {
        if (!TryPopDate(ctx, out var year2, out var month2, out var day2))
            return;

        if (!TryPopDate(ctx, out var year1, out var month1, out var day1))
            return;

        ctx.Push(ToJulianDayNumber(year2, month2, day2) - ToJulianDayNumber(year1, month1, day1));
    }

    static void CalendarDateToJulianDay(IFungeExecutionContext ctx)
    {
        if (!TryPopDate(ctx, out var year, out var month, out var day))
            return;

        ctx.Push(ToJulianDayNumber(year, month, day));
    }

    static void YearDayOfYearToDate(IFungeExecutionContext ctx)
    {
        var dayOfYear = ctx.Pop();
        var year = ctx.Pop();
        if (!TryCreateDateFromDayOfYear(year, dayOfYear, out var month, out var day))
        {
            ctx.Reflect();
            return;
        }

        PushDate(ctx, year, month, day);
    }

    static void DayOfWeek(IFungeExecutionContext ctx)
    {
        if (!TryPopDate(ctx, out var year, out var month, out var day))
            return;

        var date = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        ctx.Push(((int)date.DayOfWeek + 6) % 7);
    }

    static void DayOfYear(IFungeExecutionContext ctx)
    {
        if (!TryPopDate(ctx, out var year, out var month, out var day))
            return;

        var date = new DateTime(year, month, day, 12, 0, 0, DateTimeKind.Unspecified);
        ctx.Push(date.DayOfYear - 1);
    }

    static bool TryPopDate(IFungeExecutionContext ctx, out int year, out int month, out int day)
    {
        day = ctx.Pop();
        month = ctx.Pop();
        year = ctx.Pop();

        if (!IsValidDate(year, month, day))
        {
            ctx.Reflect();
            return false;
        }

        return true;
    }

    static void PushDate(IFungeExecutionContext ctx, int year, int month, int day)
    {
        ctx.Push(year);
        ctx.Push(month);
        ctx.Push(day);
    }

    static bool TryPushDateFromJulianDay(IFungeExecutionContext ctx, long julianDay)
    {
        if (!TryFromJulianDayNumber(julianDay, out var year, out var month, out var day))
            return false;

        PushDate(ctx, year, month, day);
        return true;
    }

    static bool IsValidDate(int year, int month, int day)
    {
        if (year is < 1 or > 9999 || month is < 1 or > 12)
            return false;

        var daysInMonth = DateTime.DaysInMonth(year, month);
        return day >= 1 && day <= daysInMonth;
    }

    static bool TryCreateDateFromDayOfYear(int year, int dayOfYear, out int month, out int day)
    {
        month = 0;
        day = 0;

        if (year is < 1 or > 9999)
            return false;

        var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
        if (dayOfYear < 0 || dayOfYear >= daysInYear)
            return false;

        var date = new DateTime(year, 1, 1, 12, 0, 0, DateTimeKind.Unspecified).AddDays(dayOfYear);
        month = date.Month;
        day = date.Day;
        return true;
    }

    static int ToJulianDayNumber(int year, int month, int day)
    {
        var a = (14 - month) / 12;
        var y = year + 4800 - a;
        var m = month + 12 * a - 3;
        return day + (153 * m + 2) / 5 + 365 * y + y / 4 - y / 100 + y / 400 - 32045;
    }

    static bool TryFromJulianDayNumber(long julianDay, out int year, out int month, out int day)
    {
        year = 0;
        month = 0;
        day = 0;

        if (julianDay is <= 0 or > int.MaxValue)
            return false;

        var l = julianDay + 68569;
        var n = 4 * l / 146097;
        l -= (146097 * n + 3) / 4;
        var i = 4000 * (l + 1) / 1461001;
        l = l - 1461 * i / 4 + 31;
        var j = 80 * l / 2447;
        day = (int)(l - 2447 * j / 80);
        l = j / 11;
        month = (int)(j + 2 - 12 * l);
        year = (int)(100 * (n - 49) + i + l);

        return IsValidDate(year, month, day) && ToJulianDayNumber(year, month, day) == (int)julianDay;
    }
}
