using System.Threading;
using LYBT.Entities.MedicalCases;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCases.Repositories
{
    /// <summary>
    /// 医案仓储 - 审计日志与打印日志
    /// ctor 与 _context/_dbSet/_logger 由主文件提供
    /// </summary>
    internal partial class MedicalCaseRepository
    {
        /// <summary>
        /// 获取医案审计日志（分页）
        /// </summary>
        public async Task<List<MedicalCaseAuditLog>> GetAuditLogsAsync(Guid medicalCaseId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return await _context.MedicalCaseAuditLogs
                .Where(l => l.MedicalCaseId == medicalCaseId && !l.IsDeleted)
                .OrderByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 统计医案审计日志总数
        /// </summary>
        public async Task<int> CountAuditLogsAsync(Guid medicalCaseId, CancellationToken cancellationToken = default)
        {
            return await _context.MedicalCaseAuditLogs
                .CountAsync(l => l.MedicalCaseId == medicalCaseId && !l.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// 添加打印日志
        /// </summary>
        public async Task AddPrintLogAsync(MedicalCasePrintLog printLog, CancellationToken cancellationToken = default)
        {
            await _context.MedicalCasePrintLogs.AddAsync(printLog, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }
    }
}
