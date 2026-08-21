using System.Globalization;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Reports.Services;

/// <summary>
/// 报表时间桶 — 趋势图的最小聚合单元。
/// </summary>
internal readonly record struct ReportBucket(DateTime Start, DateTime EndExclusive, string Label);

/// <summary>
/// 报表时间桶生成 — 按日/周/月切分 [startDate, endDate]（含端点）。
/// 周从周一开始，月从 1 号开始，首尾桶允许与区间部分重叠。
/// P2-11-3 评估：周起始为周一（与 ClinicSettingsOptions.WeekStartsOn=周一 一致）；报表按诊所本地日界（Asia/Shanghai），
/// 调用方应传入本地 Date（ReportService 已在 Controller 层将 UTC CreatedAt 转本地 Date 后再分组，上层桶生成亦用本地 Date）。
/// 后续若支持可配置 WeekStartsOn，可注入 ClinicSettingsOptions.Timezone + WeekStartsOn 到本类。
/// </summary>
internal static class ReportTimeBuckets
{
    public static List<ReportBucket> Build(DateTime startDate, DateTime endDate, ReportGranularity granularity)
    {
        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date.AddDays(1);

        var buckets = new List<ReportBucket>();
        var cursor = granularity switch
        {
            ReportGranularity.Week => StartOfWeek(rangeStart),
            ReportGranularity.Month => new DateTime(rangeStart.Year, rangeStart.Month, 1),
            _ => rangeStart
        };

        while (cursor < rangeEnd)
        {
            var next = Next(cursor, granularity);
            var label = granularity == ReportGranularity.Month
                ? cursor.ToString("yyyy-MM", CultureInfo.InvariantCulture)
                : cursor.ToString("MM-dd", CultureInfo.InvariantCulture);
            buckets.Add(new ReportBucket(cursor, next, label));
            cursor = next;
        }

        return buckets;
    }

    private static DateTime Next(DateTime current, ReportGranularity granularity) => granularity switch
    {
        ReportGranularity.Week => current.AddDays(7),
        ReportGranularity.Month => current.AddMonths(1),
        _ => current.AddDays(1)
    };

    private static DateTime StartOfWeek(DateTime date) => date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
}
