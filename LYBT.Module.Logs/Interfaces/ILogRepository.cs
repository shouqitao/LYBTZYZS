using LYBT.Models.Logs;
using LYBT.Module.Logs.Dtos;

namespace LYBT.Module.Logs.Interfaces {

    /// <summary>
    /// 日志仓储接口，负责操作日志的数据持久化
    /// </summary>
    public interface ILogRepository {

        /// <summary>
        /// 新增操作日志（写入数据库）
        /// </summary>
        /// <param name="log">日志实体</param>
        /// <returns>是否成功</returns>
        Task<bool> AddAsync(LogModel log);

        /// <summary>
        /// 分页/条件查询操作日志
        /// </summary>
        /// <param name="query">查询条件</param>
        /// <returns>日志实体分页结果</returns>
        Task<(IList<LogModel> logs, int total)> GetPagedAsync(LogQueryDto query);

        /// <summary>
        /// 根据日志ID获取详情
        /// </summary>
        /// <param name="id">日志ID</param>
        /// <returns>日志实体</returns>
        Task<LogModel?> GetByIdAsync(Guid id);
    }
}