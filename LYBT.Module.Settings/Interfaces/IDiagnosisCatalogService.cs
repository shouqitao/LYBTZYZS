using LYBT.Module.Settings.Dtos;

namespace LYBT.Module.Settings.Interfaces {

    public interface IDiagnosisCatalogService {

        Task<List<DiagnosisCatalogDto>> GetAllAsync();

        Task<bool> AddAsync(DiagnosisCatalogCreateDto dto);

        Task<bool> UpdateAsync(DiagnosisCatalogEditDto dto);

        Task<bool> DeleteAsync(Guid id);
    }
}