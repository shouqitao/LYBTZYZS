using LYBT.Module.Registration.Dtos;
using LYBT.Common.Enums;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IRegistrationService {
        Task<IList<RegistrationDto>> GetListAsync();
        Task<IList<RegistrationDto>> GetByStatusAsync(RegistrationStatus status);
        Task<RegistrationDetailDto?> GetByIdAsync(Guid id);
        Task<Guid?> AddAsync(RegistrationCreateDto dto);
        Task<bool> UpdateAsync(RegistrationEditDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> CancelAsync(Guid id);
    }
}
