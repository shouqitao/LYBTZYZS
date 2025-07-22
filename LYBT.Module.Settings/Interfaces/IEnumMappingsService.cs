namespace LYBT.Module.Settings.Interfaces {

/// <summary>
/// 表示IEnumMappingsService。
/// </summary>
    public interface IEnumMappingsService {

        Task<Dictionary<string, Dictionary<int, string>>> GetAllAsync();
    }
}
