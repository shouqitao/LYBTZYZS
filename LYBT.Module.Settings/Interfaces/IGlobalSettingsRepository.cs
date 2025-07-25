using LYBT.Module.Settings.Models;

namespace LYBT.Module.Settings.Interfaces {

    /// <summary>
    /// 表示IGlobalSettingsRepository。
    /// </summary>
    public interface IGlobalSettingsRepository {

        Task<GlobalSettingsModel?> GetAsync();

        Task<bool> SaveAsync(GlobalSettingsModel model);
    }
}