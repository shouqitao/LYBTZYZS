namespace LYBT.Shared.Models.Primitives;

/// <summary>
/// 诊所本地时间工具（P1-10 / H-2）
/// 运营日界 = 诊所本地时间（默认 Asia/Shanghai），非 UTC——避免北京 00-08 时经 UTC 跨日误锁 Completed 医案。
/// 时区可通过环境变量 LYBT_CLINIC_TIMEZONE 覆盖（如 "Asia/Shanghai"）。
/// 实体层（LYBT.Entities.MedicalCases.MedicalCaseTime）与 DTO 层共用本实现，避免重复硬编码。
/// </summary>
public static class ClinicTime
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
