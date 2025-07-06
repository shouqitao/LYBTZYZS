using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Doctors.Interfaces {

    /// <summary>
    /// 医生业务服务接口，定义医生的业务操作
    /// </summary>
    public interface IDoctorService {

        Task<DoctorDetailDto?> GetByIdAsync(Guid id);
        Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId);
        Task<List<DoctorDto>> SearchAsync(string keyword);
        Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query);
        Task<bool> AddAsync(DoctorDetailDto doctorDetailDto);
        Task<bool> UpdateAsync(DoctorDetailDto doctorDetailDto);
        Task<bool> DisableAsync(Guid id);
        Task<bool> EnableAsync(Guid id);
        Task<int> BatchDisableAsync(List<Guid> ids);
        Task<int> BatchEnableAsync(List<Guid> ids);
        Task<bool> ResetPasswordAsync(Guid id, string newPassword);
        Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
    }
}