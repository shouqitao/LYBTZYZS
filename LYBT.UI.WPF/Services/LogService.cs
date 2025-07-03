using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Logs.Dtos;
using LYBT.UI.WPF.Apis;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 LogService 的说明
    /// </summary>
    public class LogService : ILogService {
        private readonly ILogApi _api;

        public LogService(ILogApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<Guid?> AddAsync(LogDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success ? resp.Id : null;
        }

        /// <summary>
        /// 方法 GetLogsAsync 的说明
        /// </summary>
        public async Task<(IList<LogDto> Logs, int Total)> GetLogsAsync(LogQueryDto query) {
            var resp = await _api.GetLogsAsync(query);
            return (resp.Logs, resp.Total);
        }

        /// <summary>
        /// 方法 GetUserLogsAsync 的说明
        /// </summary>
        public async Task<(IList<LogDto> Logs, int Total)> GetUserLogsAsync(Guid userId, int page = 1, int pageSize = 20) {
            var resp = await _api.GetUserLogsAsync(userId, page, pageSize);
            return (resp.Logs, resp.Total);
        }

        /// <summary>
        /// 方法 GetPatientLogsAsync 的说明
        /// </summary>
        public async Task<(IList<LogDto> Logs, int Total)> GetPatientLogsAsync(Guid patientId, int page = 1, int pageSize = 20) {
            var resp = await _api.GetPatientLogsAsync(patientId, page, pageSize);
            return (resp.Logs, resp.Total);
        }

        /// <summary>
        /// 方法 GetRecordLogsAsync 的说明
        /// </summary>
        public async Task<(IList<LogDto> Logs, int Total)> GetRecordLogsAsync(Guid recordId, int page = 1, int pageSize = 20) {
            var resp = await _api.GetRecordLogsAsync(recordId, page, pageSize);
            return (resp.Logs, resp.Total);
        }

        /// <summary>
        /// 方法 GetPrescriptionLogsAsync 的说明
        /// </summary>
        public async Task<(IList<LogDto> Logs, int Total)> GetPrescriptionLogsAsync(Guid prescriptionId, int page = 1, int pageSize = 20) {
            var resp = await _api.GetPrescriptionLogsAsync(prescriptionId, page, pageSize);
            return (resp.Logs, resp.Total);
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<LogDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }
    }
}
