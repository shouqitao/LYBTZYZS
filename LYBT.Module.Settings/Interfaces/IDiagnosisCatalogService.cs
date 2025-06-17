using LYBT.Module.Settings.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface IDiagnosisCatalogService {
        Task<List<DiagnosisCatalogDto>> GetAllAsync();
        Task<bool> AddAsync(DiagnosisCatalogCreateDto dto);
        Task<bool> UpdateAsync(DiagnosisCatalogEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
