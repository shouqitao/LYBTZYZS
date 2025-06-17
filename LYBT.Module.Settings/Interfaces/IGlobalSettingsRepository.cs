using LYBT.Models.Settings;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface IGlobalSettingsRepository {
        Task<GlobalSettingsModel?> GetAsync();
        Task<bool> SaveAsync(GlobalSettingsModel model);
    }
}
