using FluentAssertions;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// ReportsController 分层守卫（P0-1 API 单一职能 2026-08-14: Remote 注入 IReportService——
/// 防回归：Controller 不得直连 Repository）
/// </summary>
public class ReportsControllerLayeringTests
{
    [Fact]
    public void RemoteReportsController_Injects_ReportService_NotRepository()
        => AssertInjectsService(typeof(LYBT.WebAPI.Controllers.ReportsController));

    private static void AssertInjectsService(Type controllerType)
    {
        var ctor = controllerType.GetConstructors().Single();
        var paramTypes = ctor.GetParameters().Select(p => p.ParameterType).ToList();

        paramTypes.Should().Contain(t => t.Name == "IReportService",
            $"{controllerType.Name} 必须注入 IReportService（DTO 组装在 Service 层——3-Layer 约束）");
        paramTypes.Should().NotContain(t => t.Name == "IReportRepository",
            $"{controllerType.Name} 不得直注 IReportRepository（跳过 Service 层——分层违规 P0-1）");
    }
}
