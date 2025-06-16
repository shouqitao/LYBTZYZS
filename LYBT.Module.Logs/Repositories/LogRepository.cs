using LYBT.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// 操作日志仓储实现类（基于EF Core）
/// </summary>
public class LogRepository : ILogRepository {
    private readonly AppDbContext _dbContext;

    /// <summary>
    /// 构造方法，注入数据库上下文
    /// </summary>
    /// <param name="dbContext">应用程序数据库上下文</param>
    public LogRepository(AppDbContext dbContext) {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 新增操作日志（写入数据库）
    /// </summary>
    /// <param name="log">日志实体</param>
    /// <returns>是否成功</returns>
    public async Task<bool> AddAsync(LogModel log) {
        await _dbContext.Logs.AddAsync(log);
        return await _dbContext.SaveChangesAsync() > 0;
    }

    /// <summary>
    /// 分页/条件查询操作日志
    /// </summary>
    /// <param name="query">查询条件</param>
    /// <returns>日志实体分页结果</returns>
    public async Task<(IList<LogModel> logs, int total)> GetPagedAsync(LogQueryDto query) {
        var dbSet = _dbContext.Logs.AsQueryable();

        // 对象类型筛选
        if (query.ObjectType.HasValue)
            dbSet = dbSet.Where(l => l.ObjectType == query.ObjectType.Value);

        // 对象ID筛选
        if (query.ObjectId.HasValue)
            dbSet = dbSet.Where(l => l.ObjectId == query.ObjectId);

        // 操作类型筛选
        if (query.ActionType.HasValue)
            dbSet = dbSet.Where(l => l.ActionType == query.ActionType.Value);

        // 操作者筛选
        if (query.OperatorId.HasValue)
            dbSet = dbSet.Where(l => l.OperatorId == query.OperatorId);

        // 日志类型筛选
        if (query.LogType.HasValue)
            dbSet = dbSet.Where(l => l.LogType == query.LogType.Value);

        // 起止时间筛选
        if (query.StartTime.HasValue)
            dbSet = dbSet.Where(l => l.LogTime >= query.StartTime.Value);
        if (query.EndTime.HasValue)
            dbSet = dbSet.Where(l => l.LogTime <= query.EndTime.Value);

        // 计算总数
        int total = await dbSet.CountAsync();

        // 分页排序（按时间倒序）
        int skip = (query.Page - 1) * query.PageSize;
        var logs = await dbSet
            .OrderByDescending(l => l.LogTime)
            .Skip(skip)
            .Take(query.PageSize)
            .ToListAsync();

        return (logs, total);
    }

    /// <summary>
    /// 根据日志ID获取详情
    /// </summary>
    /// <param name="id">日志ID</param>
    /// <returns>日志实体</returns>
    public async Task<LogModel?> GetByIdAsync(Guid id) {
        return await _dbContext.Logs.FindAsync(id);
    }
}
