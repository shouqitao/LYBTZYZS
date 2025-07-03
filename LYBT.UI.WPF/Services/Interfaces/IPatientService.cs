using LYBT.Common.Models;
using LYBT.Module.Patients.Dtos;
using LYBT.Module.Records.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IPatientService {
        Task<bool> AddAsync(PatientCreateDto dto);
        Task<bool> UpdateAsync(PatientEditDto dto);
        Task<bool> EnableAsync(Guid id);
        Task<bool> DisableAsync(Guid id);
        Task<PatientDetailDto?> GetByIdAsync(Guid id);
        Task<IList<PatientDto>> GetAllAsync();
        Task<PagedResultDto<PatientDto>> GetPagedAsync(PatientPagedQueryDto query);
        Task<int> BatchDeleteAsync(List<string> ids);
        Task<int> BatchDisableAsync(List<Guid> ids);
        Task<IList<PatientDto>> SearchAsync(string keyword);
        Task<IList<PatientDto>> GetForDoctorAsync(Guid doctorId);
        Task<bool> AssignDoctorAsync(Guid patientId, Guid doctorId);
        Task<int> ImportAsync(List<PatientCreateDto> dtos);
        Task<IList<PatientDto>> ExportAsync();
        Task<IList<RecordDto>> GetHistoryAsync(Guid patientId);
    }
}
