using LYBT.Models.Settings;

namespace LYBT.Module.Settings.Interfaces {

    public interface IGlobalSettingsRepository {

        Task<GlobalSettingsModel?> GetAsync();

        Task<bool> SaveAsync(GlobalSettingsModel model);
    }
}