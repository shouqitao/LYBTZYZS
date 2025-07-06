using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IDoctorService {
        Task<IList<DoctorDto>> SearchAsync(string keyword = "");
        Task<DoctorDetailDto?> GetByIdAsync(Guid id);
        Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId);
        Task<bool> AddAsync(DoctorDetailDto dto);
        Task<bool> UpdateAsync(DoctorDetailDto dto);
        Task<bool> DisableAsync(Guid id);
        Task<bool> EnableAsync(Guid id);
        Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query);
        Task<int> BatchDisableAsync(List<Guid> ids);
        Task<int> BatchEnableAsync(List<Guid> ids);
        Task<IList<string>> GetRolesAsync();
    }
}