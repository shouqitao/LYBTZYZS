namespace LYBT.Entities.MedicalCases;

/// <summary>
/// 医案时间边界（P1-10）
/// 运营日界 = 诊所本地时间（默认 Asia/Shanghai），非 UTC——避免北京 00-08 时经 UTC 跨日误锁 Completed 医案。
/// 时区可通过环境变量 LYBT_CLINIC_TIMEZONE" 覆盖（如 "Asia/Shanghai"）。
/// </summary>
public static class MedicalCaseTime
{
    private static readonly TimeZoneInfo ClinicTz = ResolveTimezone();

    private static TimeZoneInfo ResolveTimezone()
    {
        try
        {
            var id = Environment.GetEnvironmentVariable("LYBT_CLINIC_TIMEZONE");
            if (string.IsNullOrWhiteSpace(id))
                id = "Asia/Shanghai";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
            if (tz != null) return tz;
        }
        catch { }
        return TimeZoneInfo.Utc;
    }

    /// <summary>UTC 时间 → 诊所本地日期</summary>
    public static DateTime ClinicLocalDate(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ClinicTz);
        return local.Date;
    }
}
