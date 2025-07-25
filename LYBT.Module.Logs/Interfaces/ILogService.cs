using LYBT.Common.Enums.Users;
using LYBT.Module.Logs.Models.Dtos;

namespace LYBT.Module.Logs.Interfaces {

    /// <summary>
    /// 日志服务接口，负责业务层日志写入与查询逻辑
    /// </summary>
    public interface ILogService {

        /// <summary>
        /// 写入一条操作日志（可被任意业务模块调用）
        /// </summary>
        /// <param name="logDto">日志DTO</param>
        /// <returns>写入后日志ID</returns>
        Task<Guid> AddLogAsync(LogDto logDto);

        /// <summary>
        /// 分页/条件查询操作日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>分页结果，含总数</returns>
        Task<(IList<LogDto> logs, int total)> GetLogsAsync(LogQueryDto query);

        /// <summary>
        /// 根据当前用户角色过滤日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <param name="role">当前用户角色</param>
        /// <param name="userId">当前用户Id</param>
        /// <returns>分页结果</returns>
        Task<(IList<LogDto> logs, int total)> GetLogsAsync(LogQueryDto query, UserRole role, Guid userId);

        /// <summary>
        /// 获取指定用户的操作日志
        /// </summary>
        Task<(IList<LogDto> logs, int total)> GetUserLogsAsync(Guid userId, int page, int pageSize);

        /// <summary>
        /// 获取指定患者的操作日志
        /// </summary>
        Task<(IList<LogDto> logs, int total)> GetPatientLogsAsync(Guid patientId, int page, int pageSize);

        /// <summary>
        /// 获取指定病历的操作日志
        /// </summary>
        Task<(IList<LogDto> logs, int total)> GetRecordLogsAsync(Guid recordId, int page, int pageSize);

        /// <summary>
        /// 获取指定处方的操作日志
        /// </summary>
        Task<(IList<LogDto> logs, int total)> GetPrescriptionLogsAsync(Guid prescriptionId, int page, int pageSize);

        /// <summary>
        /// 根据日志ID获取详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>日志DTO</returns>
        Task<LogDto?> GetLogByIdAsync(Guid id);
    }
}