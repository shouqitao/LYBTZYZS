using LYBT.Module.Logs.Dtos;
using Refit;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Apis {
    public interface ILogApi {
        [Post("/api/Log")]
        Task<AddLogResponse> AddAsync([Body] LogDto dto);

        [Get("/api/Log")]
        Task<GetLogsResponse> GetLogsAsync([Query] LogQueryDto query);

        [Get("/api/Log/user/{userId}")]
        Task<GetLogsResponse> GetUserLogsAsync(Guid userId, [Query] int page = 1, [Query] int pageSize = 20);

        [Get("/api/Log/patient/{patientId}")]
        Task<GetLogsResponse> GetPatientLogsAsync(Guid patientId, [Query] int page = 1, [Query] int pageSize = 20);

        [Get("/api/Log/record/{recordId}")]
        Task<GetLogsResponse> GetRecordLogsAsync(Guid recordId, [Query] int page = 1, [Query] int pageSize = 20);

        [Get("/api/Log/prescription/{prescriptionId}")]
        Task<GetLogsResponse> GetPrescriptionLogsAsync(Guid prescriptionId, [Query] int page = 1, [Query] int pageSize = 20);

        [Get("/api/Log/{id}")]
        Task<LogDto> GetByIdAsync(Guid id);
    }
}
