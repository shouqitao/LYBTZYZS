using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Interfaces {

    public interface IGlobalSettingsService {

        Task<GlobalSettingsDto?> GetAsync();

        Task<bool> SaveAsync(GlobalSettingsDto dto);
    }
}