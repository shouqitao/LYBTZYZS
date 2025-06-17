using LYBT.Module.Settings.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface ITreatmentCatalogService {
        Task<List<TreatmentCatalogDto>> GetAllAsync();
        Task<bool> AddAsync(TreatmentCatalogCreateDto dto);
        Task<bool> UpdateAsync(TreatmentCatalogEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
