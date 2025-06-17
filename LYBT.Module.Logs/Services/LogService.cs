using LYBT.Models.Logs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;
using LYBT.Common.Enums.Logs;

namespace LYBT.Module.Logs.Services {
    /// <summary>
    /// 日志服务实现类
    /// </summary>
    public class LogService : ILogService {
        private readonly ILogRepository _logRepository;

        /// <summary>
        /// 构造方法，注入日志仓储接口
        /// </summary>
        /// <param name="logRepository">日志仓储实现</param>
        public LogService(ILogRepository logRepository) {
            _logRepository = logRepository;
        }

        /// <inheritdoc />
        public async Task<Guid> AddLogAsync(LogDto logDto) {
            var log = new LogModel {
                Id = Guid.NewGuid(),
                LogType = logDto.LogType,
                ObjectType = logDto.ObjectType,
                ObjectId = logDto.ObjectId,
                ActionType = logDto.ActionType,
                OperatorId = logDto.OperatorId,
                OperatorName = logDto.OperatorName,
                LogTime = logDto.LogTime,
                Content = logDto.Content,
                OldValue = logDto.OldValue,
                NewValue = logDto.NewValue,
                IP = logDto.IP,
                Remark = logDto.Remark
            };
            var result = await _logRepository.AddAsync(log);
            return result ? log.Id : Guid.Empty;
        }

        /// <inheritdoc />
        public async Task<(IList<LogDto> logs, int total)> GetLogsAsync(LogQueryDto query) {
            var (logs, total) = await _logRepository.GetPagedAsync(query);
            var list = new List<LogDto>();
            foreach (var log in logs) {
                list.Add(new LogDto {
                    Id = log.Id,
                    LogType = log.LogType,
                    ObjectType = log.ObjectType,
                    ObjectId = log.ObjectId,
                    ActionType = log.ActionType,
                    OperatorId = log.OperatorId,
                    OperatorName = log.OperatorName,
                    LogTime = log.LogTime,
                    Content = log.Content,
                    OldValue = log.OldValue,
                    NewValue = log.NewValue,
                    IP = log.IP,
                    Remark = log.Remark
                });
            }
            return (list, total);
        }

        /// <inheritdoc />
        public async Task<(IList<LogDto> logs, int total)> GetUserLogsAsync(Guid userId, int page, int pageSize) {
            var query = new LogQueryDto {
                ObjectType = ObjectType.User,
                ObjectId = userId,
                Page = page,
                PageSize = pageSize
            };
            return await GetLogsAsync(query);
        }

        /// <inheritdoc />
        public async Task<(IList<LogDto> logs, int total)> GetPatientLogsAsync(Guid patientId, int page, int pageSize) {
            var query = new LogQueryDto {
                ObjectType = ObjectType.Patient,
                ObjectId = patientId,
                Page = page,
                PageSize = pageSize
            };
            return await GetLogsAsync(query);
        }

        /// <inheritdoc />
        public async Task<LogDto?> GetLogByIdAsync(Guid id) {
            var log = await _logRepository.GetByIdAsync(id);
            if (log == null)
                return null;
            return new LogDto {
                Id = log.Id,
                LogType = log.LogType,
                ObjectType = log.ObjectType,
                ObjectId = log.ObjectId,
                ActionType = log.ActionType,
                OperatorId = log.OperatorId,
                OperatorName = log.OperatorName,
                LogTime = log.LogTime,
                Content = log.Content,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                IP = log.IP,
                Remark = log.Remark
            };
        }
    }
}
