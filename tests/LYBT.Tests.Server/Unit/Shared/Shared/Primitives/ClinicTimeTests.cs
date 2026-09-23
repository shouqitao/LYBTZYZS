using LYBT.Shared.Models.Primitives;

namespace LYBT.Tests.Server;

/// <summary>
/// ClinicTime 运营日界单测（2026-09-24 回归守卫）。
/// <para>背景：仓库既定规则是「运营日界 = 诊所本地时间（默认 Asia/Shanghai），非 UTC」，
/// 但挂号队列「仅当天」过滤、同日查重、当日最大号、取消原因判定、编号日期段曾分别用
/// <c>DateTime.Today</c>（宿主本地）/<c>DateTime.UtcNow.Date</c>（UTC）——三套日界不一致，
/// 导致诊所 00:00-08:00 的挂号漏出队列、医案被误判「非当天」而要求取消原因（3 个 E2E 红）。
/// 本测试锁定日界换算本身，使各处可共用同一来源。</para>
/// </summary>
public class ClinicTimeTests
{
    [Fact]
    public void ClinicLocalDayRangeUtc_ContainsTheGivenInstant()
    {
        var utc = new DateTime(2026, 9, 23, 22, 6, 15, DateTimeKind.Utc);

        var (start, end) = ClinicTime.ClinicLocalDayRangeUtc(utc);

        start.Should().BeOnOrBefore(utc);
        utc.Should().BeBefore(end);
    }

    [Fact]
    public void ClinicLocalDayRangeUtc_StartIsClinicLocalMidnight_AndEndIsNextClinicLocalMidnight()
    {
        var utc = new DateTime(2026, 9, 23, 22, 6, 15, DateTimeKind.Utc);

        var (start, end) = ClinicTime.ClinicLocalDayRangeUtc(utc);

        // 区间起点在诊所本地恰为 00:00，且与前一刻分属不同诊所日（日界正确落点）
        ClinicTime.ClinicLocalDate(start).Should().Be(ClinicTime.ClinicLocalDate(utc));
        ClinicTime.ClinicLocalDate(start.AddSeconds(-1))
            .Should().BeBefore(ClinicTime.ClinicLocalDate(start), "区间起点必须是诊所本地当日的第一刻");

        // 终点是下一诊所日的起点（区间为半开 [start, end)）
        ClinicTime.ClinicLocalDate(end).Should().BeAfter(ClinicTime.ClinicLocalDate(start));
        ClinicTime.ClinicLocalDate(end.AddSeconds(-1)).Should().Be(ClinicTime.ClinicLocalDate(start));
    }

    [Fact]
    public void ClinicLocalDayRangeUtc_IsStableAcrossTheUtcMidnightBoundary()
    {
        // 关键回归场景：UTC 22:00（诊所次日 06:00）与 UTC 次日 01:00 属于同一个诊所日，
        // 而 UTC 日期已跨天——旧实现（UtcNow.Date / DateTime.Today）在此必然给出不同日界。
        var beforeUtcMidnight = new DateTime(2026, 9, 23, 22, 0, 0, DateTimeKind.Utc);
        var afterUtcMidnight = new DateTime(2026, 9, 24, 1, 0, 0, DateTimeKind.Utc);

        var (startA, endA) = ClinicTime.ClinicLocalDayRangeUtc(beforeUtcMidnight);
        var (startB, endB) = ClinicTime.ClinicLocalDayRangeUtc(afterUtcMidnight);

        startA.Should().Be(startB);
        endA.Should().Be(endB);
    }

    [Fact]
    public void ClinicLocalDate_UsesClinicTimeZone_NotUtcDate()
    {
        // UTC 22:00 → 诊所（UTC+8）已是次日；显式钉住「日界非 UTC」这一约定
        var utc = new DateTime(2026, 9, 23, 22, 0, 0, DateTimeKind.Utc);

        ClinicTime.ClinicLocalDate(utc).Should().NotBe(utc.Date);
        ClinicTime.ClinicLocalDate(utc).Should().Be(utc.Date.AddDays(1));
    }
}
