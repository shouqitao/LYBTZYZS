using LYBT.Module.Registration.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IRegistrationService {
        Task<IList<RegistrationDto>> GetListAsync();
        Task<RegistrationDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(RegistrationCreateDto dto);
        Task<bool> UpdateAsync(RegistrationEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
