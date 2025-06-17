using LYBT.Module.Settings.Dtos;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface IGlobalSettingsService {
        Task<GlobalSettingsDto?> GetAsync();
        Task<bool> SaveAsync(GlobalSettingsDto dto);
    }
}
