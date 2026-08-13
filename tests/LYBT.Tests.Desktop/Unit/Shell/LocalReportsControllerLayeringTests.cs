using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Desktop.Unit.Shell;

/// <summary>
/// Local ReportsController 分层守卫（P0-1 API 单一职能 2026-08-14: 原直注 IReportRepository
/// 跳过 Service 层——已改为注入 IReportService 与 Remote 一致——防回归）
/// </summary>
public class LocalReportsControllerLayeringTests
{
    [Fact]
    public void LocalReportsController_Injects_ReportService_NotRepository()
    {
        var ctor = typeof(LYBT.LocalWebAPI.Controllers.ReportsController)
            .GetConstructors()
            .Single();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        paramTypes.Should().Contain(t => t.Name == "IReportService",
            "Local ReportsController 必须注入 IReportService（DTO 组装在 Service 层——3-Layer 约束）");
        paramTypes.Should().NotContain(t => t.Name == "IReportRepository",
            "Local ReportsController 不得直注 IReportRepository（跳过 Service 层——分层违规 P0-1）");
    }
}
