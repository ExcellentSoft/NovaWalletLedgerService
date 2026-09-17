namespace NovaWallet.Application.Common;

/// <summary>
/// Helper for the "reset at midnight WAT" (West Africa Time, UTC+1, no DST) requirement on the daily
/// outbound transfer limit, without pulling in a full timezone database dependency.
/// </summary>
public static class CustomWestAfricaDateTime
{
    public static readonly TimeSpan Offset = TimeSpan.FromHours(1);

    public static DateTime UtcNowAsWat() => DateTime.UtcNow + Offset;

    public static DateOnly TodayWat() => DateOnly.FromDateTime(UtcNowAsWat());
}
