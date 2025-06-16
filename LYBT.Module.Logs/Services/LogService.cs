using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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

    /// <summary>
    /// 写入一条操作日志
    /// </summary>
    /// <param name="logDto">日志DTO</param>
    /// <returns>写入后日志ID</returns>
    public async Task<Guid> AddLogAsync(LogDto logDto) {
        // DTO转实体
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

    /// <summary>
    /// 分页/条件查询操作日志
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>分页结果</returns>
    public async Task<(IList<LogDto> logs, int total)> GetLogsAsync(LogQueryDto query) {
        var (logs, total) = await _logRepository.GetPagedAsync(query);
        // 实体转DTO
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

    /// <summary>
    /// 根据日志ID获取详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <returns>日志DTO</returns>
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
