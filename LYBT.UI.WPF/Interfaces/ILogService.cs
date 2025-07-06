using LYBT.Module.Logs.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface ILogService {
        Task<Guid?> AddAsync(LogDto dto);
        Task<(IList<LogDto> Logs, int Total)> GetLogsAsync(LogQueryDto query);
        Task<(IList<LogDto> Logs, int Total)> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 20);
        Task<(IList<LogDto> Logs, int Total)> GetPatientLogsAsync(Guid patientId, int page = 1, int pageSize = 20);
        Task<(IList<LogDto> Logs, int Total)> GetRecordLogsAsync(Guid recordId, int page = 1, int pageSize = 20);
        Task<(IList<LogDto> Logs, int Total)> GetPrescriptionLogsAsync(Guid prescriptionId, int page = 1, int pageSize = 20);
        Task<LogDto?> GetByIdAsync(Guid id);
    }
}
