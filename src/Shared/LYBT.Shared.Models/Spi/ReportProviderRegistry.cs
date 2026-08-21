namespace LYBT.Shared.Models.Spi;

/// <summary>报表 SPI 注册表 — 聚合所有已注册的 <see cref="IReportProvider"/>，供报表路由/菜单动态发现。</summary>
public sealed class ReportProviderRegistry
{
    private readonly IReadOnlyList<IReportProvider> _providers;

    public ReportProviderRegistry(IEnumerable<IReportProvider> providers) => _providers = providers.ToList();

    public IReadOnlyList<IReportProvider> All => _providers;

    public IReportProvider? Find(string code) => _providers.FirstOrDefault(p => p.Code == code);
}
