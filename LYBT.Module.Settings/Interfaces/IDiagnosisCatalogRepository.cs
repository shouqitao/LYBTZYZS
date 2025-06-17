using LYBT.Models.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface IDiagnosisCatalogRepository {
        Task<List<DiagnosisCatalogModel>> GetAllAsync();
        Task<bool> AddAsync(DiagnosisCatalogModel model);
        Task<bool> UpdateAsync(DiagnosisCatalogModel model);
        Task<bool> DeleteAsync(Guid id);
    }
}
