using LYBT.Entities.MedicalCases;
using LYBT.Module.MedicalCases.Services;

namespace LYBT.Tests.Server.Unit.MedicalCase;

/// <summary>
/// P1-10 医案日界时区验证：运营日界 = 诊所本地时间（Asia/Shanghai），非 UTC
/// </summary>
public class MedicalCaseTimeTests
{
    private static LYBT.Entities.MedicalCases.MedicalCase CompletedAt(DateTime utc)
    {
        var mc = new LYBT.Entities.MedicalCases.MedicalCase { CaseStatus = LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed };
        mc.CompletedAt = utc;
        return mc;
    }

    [Theory]
    // 北京 2026-08-21 02:00Z == 当天 10:00 本地 → 未跨日，不锁定
    [InlineData("2026-08-21T02:00:00Z", "2026-08-21T10:00:00Z", false)]
    // 本地 08-20 完成（UTC 08-19 16:00 == 本地 08-20 00:00），现本地 08-21 → 跨日锁定
    [InlineData("2026-08-19T16:00:00Z", "2026-08-21T10:00:00Z", true)]
    // 关键回归：UTC 已是 08-21 但本地仍 08-20（北京时间 00-08 窗口）→ 不锁定（P1-10 修复目标）
    [InlineData("2026-08-20T16:00:00Z", "2026-08-20T20:00:00Z", false)]
    // 同日本地 08-21 02:00Z == 10:00 本地 vs 完成 08-21 03:00Z == 11:00 本地 → 未跨日
    [InlineData("2026-08-21T03:00:00Z", "2026-08-21T10:00:00Z", false)]
    public void IsLocked_Uses_ClinicLocal_DateBoundary(string completedUtc, string nowUtc, bool expected)
    {
        var mc = CompletedAt(DateTime.Parse(completedUtc).ToUniversalTime());
        var now = DateTimeOffset.Parse(nowUtc).ToUniversalTime();
        var svc = new MedicalCaseTimeService();
        Assert.Equal(expected, svc.IsLocked(mc, now));
    }

    [Fact]
    public void ClinicLocalDate_Shanghai()
    {
        // 北京 2026-08-21 00:00（= UTC 2026-08-20 16:00），本地日期应为 08-21
        var utc = new DateTime(2026, 8, 20, 16, 0, 0, DateTimeKind.Utc);
        Assert.Equal(new DateTime(2026, 8, 21), MedicalCaseTime.ClinicLocalDate(utc));
    }
}
