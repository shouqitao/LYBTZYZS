using LYBT.Module.Settings.Models;

namespace LYBT.Module.Settings.Interfaces {

    /// <summary>
    /// 表示IDiagnosisCatalogRepository。
    /// </summary>
    public interface IDiagnosisCatalogRepository {

        Task<List<DiagnosisCatalogModel>> GetAllAsync();

        Task<bool> AddAsync(DiagnosisCatalogModel model);

        Task<bool> UpdateAsync(DiagnosisCatalogModel model);

        Task<bool> DeleteAsync(Guid id);
    }
}