namespace LYBT.Shared.Models.Spi;

/// <summary>SPI：报表扩展点。新增报表仅需实现此接口并注册为 <see cref="IReportProvider"/>，无需改动现有 <c>ReportService</c>。</summary>
public interface IReportProvider
{
    /// <summary>报表唯一编码（如 daily-income、consultation-trend）。</summary>
    string Code { get; }

    /// <summary>显示名称。</summary>
    string DisplayName { get; }
}
