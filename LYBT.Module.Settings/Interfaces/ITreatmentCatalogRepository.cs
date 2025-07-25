using LYBT.Module.Settings.Models;

namespace LYBT.Module.Settings.Interfaces {

    /// <summary>
    /// 表示ITreatmentCatalogRepository。
    /// </summary>
    public interface ITreatmentCatalogRepository {

        Task<List<TreatmentCatalogModel>> GetAllAsync();

        Task<bool> AddAsync(TreatmentCatalogModel model);

        Task<bool> UpdateAsync(TreatmentCatalogModel model);

        Task<bool> DeleteAsync(Guid id);
    }
}