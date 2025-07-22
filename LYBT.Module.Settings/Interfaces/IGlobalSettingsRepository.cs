using LYBT.Models.Settings;

namespace LYBT.Module.Settings.Interfaces {

/// <summary>
/// 表示IGlobalSettingsRepository。
/// </summary>
    public interface IGlobalSettingsRepository {

        Task<GlobalSettingsModel?> GetAsync();

        Task<bool> SaveAsync(GlobalSettingsModel model);
    }
}
