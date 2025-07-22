using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Interfaces {

/// <summary>
/// 表示ITreatmentCatalogService。
/// </summary>
    public interface ITreatmentCatalogService {

        Task<List<TreatmentCatalogDto>> GetAllAsync();

        Task<bool> AddAsync(TreatmentCatalogCreateDto dto);

        Task<bool> UpdateAsync(TreatmentCatalogEditDto dto);

        Task<bool> DeleteAsync(Guid id);
    }
}
