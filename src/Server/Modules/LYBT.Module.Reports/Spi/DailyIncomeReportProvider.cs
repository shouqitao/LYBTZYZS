using LYBT.Shared.Models.Spi;

namespace LYBT.Module.Reports.Spi;

/// <summary>示例报表提供方 — 演示新增报表仅需新增类 + DI 注册，不改现有 ReportService。</summary>
public sealed class DailyIncomeReportProvider : IReportProvider
{
    public string Code => "daily-income";
    public string DisplayName => "每日收入";
}
