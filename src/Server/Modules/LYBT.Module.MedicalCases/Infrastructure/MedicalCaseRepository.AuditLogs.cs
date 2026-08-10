using LYBT.Entities.MedicalCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCases.Infrastructure
{
    /// <summary>
    /// 医案仓储 - 审计日志与打印日志
    /// ctor 与 _context/_dbSet/_logger 由主文件提供
    /// </summary>
    public partial class MedicalCaseRepository
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
        /// 记录医案审计日志（US-MC-017）
        /// 审计表无 FK 约束，医案物理删除后审计记录仍保留用于统计
        /// </summary>
        public async Task AddAuditLogAsync(MedicalCaseAuditLog log, CancellationToken cancellationToken = default)
        {
            await _context.MedicalCaseAuditLogs.AddAsync(log, cancellationToken);
            await SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 物理删除医案（US-MC-014 取消语义）
        /// Remove 聚合根，Consultation/Prescription/PrescriptionItems/PrintLogs 由 DB 级联删除
        /// </summary>
        public async Task<bool> HardDeleteAsync(MedicalCase entity, CancellationToken cancellationToken = default)
        {
            if (entity == null)
                return false;

            _context.MedicalCases.Remove(entity);
            await SaveChangesAsync(cancellationToken);
            _logger.LogInformation("[REPO] MedicalCase.HardDelete - MedicalCaseId={MedicalCaseId}", entity.Id);
            return true;
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
