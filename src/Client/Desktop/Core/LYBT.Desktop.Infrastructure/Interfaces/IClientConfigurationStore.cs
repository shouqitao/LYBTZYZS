namespace LYBT.Desktop.Infrastructure.Interfaces;

/// <summary>
/// 客户端配置存储（SHELL-018 Phase 2: 节级原子写——appsettings/clinic-settings/feature-toggles.json）
/// </summary>
public interface IClientConfigurationStore
{
    /// <summary>按节保存配置（节级覆盖，保留其他节；原子写 + .bak 备份 + IConfiguration.Reload）</summary>
    Task<bool> SaveSectionAsync(string section, IReadOnlyDictionary<string, object> values);

    /// <summary>节对应的 JSON 文件路径（ClinicSettings→clinic-settings.json、FeatureToggles→feature-toggles.json、其余→appsettings.json）</summary>
    string ResolveFilePath(string section);
}
