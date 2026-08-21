using FluentAssertions;
using LYBT.Module.Reports.Interfaces;
using System.Reflection;

namespace LYBT.Tests.Server.Unit.Reports;

/// <summary>
/// P1-23 报表行级过滤验证：
/// - IReportRepository / IReportService 全部方法均接受 Guid? doctorIdFilter（null=Admin 全量，非 null=Doctor 仅本人）
/// - ReportsController 解析角色，Doctor 传入本人 ID、Admin 传 null（见 GetDoctorFilter）
/// </summary>
public class ReportsRowLevelTests
{
    [Fact]
    public void ReportRepository_All_Methods_Accept_DoctorIdFilter()
    {
        var repo = typeof(IReportRepository);
        var methods = repo.GetMethods();
        Assert.NotEmpty(methods);

        foreach (var m in methods)
        {
            var hasFilter = m.GetParameters().Any(p => p.Name == "doctorIdFilter" && p.ParameterType == typeof(Guid?));
            hasFilter.Should().BeTrue($"IReportRepository.{m.Name} 应接受 Guid? doctorIdFilter（P1-23 行级）");
        }
    }

    [Fact]
    public void ReportService_All_Methods_Accept_DoctorIdFilter()
    {
        var svc = typeof(IReportService);
        foreach (var m in svc.GetMethods())
        {
            var hasFilter = m.GetParameters().Any(p => p.Name == "doctorIdFilter" && p.ParameterType == typeof(Guid?));
            hasFilter.Should().BeTrue($"IReportService.{m.Name} 应接受 Guid? doctorIdFilter（P1-23 行级）");
        }
    }

    [Fact]
    public void Remote_ReportsController_Resolves_DoctorFilter()
    {
        var ctl = typeof(LYBT.WebAPI.Controllers.ReportsController);
        var helper = ctl.GetMethod("GetDoctorFilter", BindingFlags.NonPublic | BindingFlags.Instance);
        helper.Should().NotBeNull("ReportsController 应提供 GetDoctorFilter（角色解析行级过滤）");
    }
}
