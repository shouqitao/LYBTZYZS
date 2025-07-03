using LYBT.Module.Pharmacy.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IPharmacyService {
        Task<IList<PharmacyDto>> GetWaitingListAsync();
        Task<IList<PharmacyDto>> GetListAsync();
        Task<PharmacyDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(PharmacyCreateDto dto);
        Task<bool> UpdateAsync(PharmacyEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsPreparedAsync(Guid id);
    }
}
