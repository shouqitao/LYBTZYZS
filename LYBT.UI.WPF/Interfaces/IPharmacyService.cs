using LYBT.Module.Pharmacy.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Common.Enums;

namespace LYBT.UI.WPF.Interfaces {
    public interface IPharmacyService {
        Task<IList<PharmacyDto>> GetWaitingListAsync();
        Task<IList<PharmacyDto>> GetByStatusAsync(PharmacyStatus status);
        Task<IList<PharmacyDto>> GetListAsync();
        Task<PharmacyDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(PharmacyCreateDto dto);
        Task<bool> UpdateAsync(PharmacyEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> MarkAsPreparedAsync(Guid id);
    }
}
