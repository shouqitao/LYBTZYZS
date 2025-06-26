using LYBT.Models.Settings;

namespace LYBT.Module.Settings.Interfaces {

    public interface IDiagnosisCatalogRepository {

        Task<List<DiagnosisCatalogModel>> GetAllAsync();

        Task<bool> AddAsync(DiagnosisCatalogModel model);

        Task<bool> UpdateAsync(DiagnosisCatalogModel model);

        Task<bool> DeleteAsync(Guid id);
    }
}