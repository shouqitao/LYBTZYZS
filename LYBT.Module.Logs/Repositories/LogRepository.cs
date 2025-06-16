using LYBT.Infrastructure;
using LYBT.Models.Logs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Module.Logs.Dtos;
using LYBT.Module.Logs.Interfaces;

namespace LYBT.Module.Logs.Repositories {
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

        /// <inheritdoc />
        public async Task<bool> AddAsync(LogModel log) {
            await _dbContext.Logs.AddAsync(log);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        /// <inheritdoc />
        public async Task<(IList<LogModel> logs, int total)> GetPagedAsync(LogQueryDto query) {
            var dbSet = _dbContext.Logs.AsQueryable();

            if (query.ObjectType.HasValue)
                dbSet = dbSet.Where(l => l.ObjectType == query.ObjectType.Value);
            if (query.ObjectId.HasValue)
                dbSet = dbSet.Where(l => l.ObjectId == query.ObjectId);
            if (query.ActionType.HasValue)
                dbSet = dbSet.Where(l => l.ActionType == query.ActionType.Value);
            if (query.OperatorId.HasValue)
                dbSet = dbSet.Where(l => l.OperatorId == query.OperatorId);
            if (query.LogType.HasValue)
                dbSet = dbSet.Where(l => l.LogType == query.LogType.Value);
            if (query.StartTime.HasValue)
                dbSet = dbSet.Where(l => l.LogTime >= query.StartTime.Value);
            if (query.EndTime.HasValue)
                dbSet = dbSet.Where(l => l.LogTime <= query.EndTime.Value);

            int total = await dbSet.CountAsync();
            int skip = (query.Page - 1) * query.PageSize;
            var logs = await dbSet
                .OrderByDescending(l => l.LogTime)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync();

            return (logs, total);
        }

        /// <inheritdoc />
        public async Task<LogModel?> GetByIdAsync(Guid id) {
            return await _dbContext.Logs.FindAsync(id);
        }
    }
}
