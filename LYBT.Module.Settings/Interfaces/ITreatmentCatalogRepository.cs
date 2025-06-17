using LYBT.Models.Settings;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Interfaces {
    public interface ITreatmentCatalogRepository {
        Task<List<TreatmentCatalogModel>> GetAllAsync();
        Task<bool> AddAsync(TreatmentCatalogModel model);
        Task<bool> UpdateAsync(TreatmentCatalogModel model);
        Task<bool> DeleteAsync(Guid id);
    }
}
