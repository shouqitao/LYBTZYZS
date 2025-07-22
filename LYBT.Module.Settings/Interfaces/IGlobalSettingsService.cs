using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Interfaces {

/// <summary>
/// 表示IGlobalSettingsService。
/// </summary>
    public interface IGlobalSettingsService {

        Task<GlobalSettingsDto?> GetAsync();

        Task<bool> SaveAsync(GlobalSettingsDto dto);
    }
}
