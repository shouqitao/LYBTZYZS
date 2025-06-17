using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface IEnumMappingsService {
        Task<Dictionary<string, Dictionary<int, string>>> GetAllAsync();
    }
}
