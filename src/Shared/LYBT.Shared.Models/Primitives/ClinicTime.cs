namespace LYBT.Shared.Models.Primitives;

/// <summary>
/// 诊所本地时间工具（P1-10 / H-2）
/// 运营日界 = 诊所本地时间（默认 Asia/Shanghai），非 UTC——避免北京 00-08 时经 UTC 跨日误锁 Completed 医案。
/// 时区可通过环境变量 LYBT_CLINIC_TIMEZONE 覆盖（如 "Asia/Shanghai"）。
/// 实体层（LYBT.Entities.MedicalCases.MedicalCaseTime）与 DTO 层共用本实现，避免重复硬编码。
/// </summary>
public static class ClinicTime
{
    // H-11: 缓存支持失效——每次调用解析环境变量，id 变化时重建缓存（避免 static readonly 永久固化）
    private static readonly object TzLock = new();
    private static TimeZoneInfo? _cachedTz;
    private static string? _cachedTzId;

    private static TimeZoneInfo ResolveTimezone()
    {
        var id = Environment.GetEnvironmentVariable("LYBT_CLINIC_TIMEZONE");
        if (string.IsNullOrWhiteSpace(id))
            id = "Asia/Shanghai";

        lock (TzLock)
        {
            if (_cachedTz != null && string.Equals(_cachedTzId, id, StringComparison.Ordinal))
                return _cachedTz;

            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById(id);
                if (tz != null)
                {
                    _cachedTz = tz;
                    _cachedTzId = id;
                    return tz;
                }
            }
            catch { }

            _cachedTz = TimeZoneInfo.Utc;
            _cachedTzId = id;
            return _cachedTz;
        }
    }

    /// <summary>UTC 时间 → 诊所本地日期</summary>
    public static DateTime ClinicLocalDate(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, ResolveTimezone());
        return local.Date;
    }

    /// <summary>
    /// 诊所本地「今日」的 UTC 起点（运营日界 = 诊所本地 00:00 对应的 UTC 瞬时）。
    /// <para>用途：EF 查询里对 UTC 存储列做「当天」过滤——必须先在 C# 侧算出区间常量再比较，
    /// 不可对列套时区转换（EF 无法翻译）。日界语义与 <see cref="ClinicLocalDate"/> 一致。</para>
    /// </summary>
    public static DateTime ClinicLocalDayStartUtc(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        var tz = ResolveTimezone();
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(utc, tz).Date;
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified), tz);
    }

    /// <summary>
    /// 诊所本地「今日」的 UTC 半开区间 <c>[StartUtc, EndUtc)</c>——与 <see cref="ClinicLocalDayStartUtc"/> 同源，
    /// 逐日换算以兼容有夏令时的时区。
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) ClinicLocalDayRangeUtc(DateTime utc)
    {
        if (utc.Kind == DateTimeKind.Unspecified)
            utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);

        return (ClinicLocalDayStartUtc(utc), ClinicLocalDayStartUtc(utc.AddDays(1)));
    }
}
