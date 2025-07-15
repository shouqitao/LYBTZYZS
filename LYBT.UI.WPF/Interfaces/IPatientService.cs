using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IPatientService {
        Task<PatientDetailDto?> GetByIdAsync(Guid id);
        Task<IList<PatientDetailDto>> GetAllAsync();
        Task<IList<PatientDetailDto>> SearchAsync(string keyword);
        Task<PagedResultDto<PatientDetailDto>> GetPagedAsync(PatientPagedQueryDto query);
        Task<bool> AddAsync(PatientDetailDto dto);
        Task<bool> UpdateAsync(PatientDetailDto dto);
        Task<bool> DeleteAsync(Guid id);
        // 可根据需要扩展更多方法
    }
}
