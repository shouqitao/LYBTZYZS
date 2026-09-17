using LYBT.Infrastructure.Data;
using LYBT.Module.Reports.Interfaces;
using LYBT.Shared.Models.Contracts.Reports;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Reports.Infrastructure;

/// <summary>
/// 报表仓储实现 — 主文件（T4.3：药材主题拆至 ReportRepository.Herbs.cs partial）。
/// </summary>
public partial class ReportRepository : IReportRepository
{
    // 架构测试 P18 显式豁免：报表需要跨模块聚合查询（Registrations + MedicalCases + Prescriptions + PrescriptionItems），
    // 注入统一 AppDbContext 是有意设计，不是违规。模块级 DbContext 仅做单模块逻辑隔离，无法支撑报表的跨模块 JOIN。
    private readonly AppDbContext _context;

    public ReportRepository(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc/>
    public async Task<decimal> GetRegistrationFeeTotalAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Registrations.Where(r =>
                !r.IsDeleted
                && r.CreatedAt >= startDate && r.CreatedAt < endDate.AddDays(1)
                && (!doctorIdFilter.HasValue || r.DoctorId == doctorIdFilter.Value)
            )
            .SumAsync(r => r.RegistrationFee, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<decimal> GetMedicineFeeTotalAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        // R-25: 单次 JOIN 查询（对齐 GetMedicineFeeByDayAsync 模式）——原两段式先取 caseIds
        // 再 IN 子句，大时间范围下 ID 列表巨大（可能触及 SQL 参数上限）且多一次往返。
        return await (
            from mc in _context.MedicalCases
            where
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
            join p in _context.Prescriptions on mc.Id equals p.MedicalCaseId
            where !p.IsDeleted
            join pi in _context.PrescriptionItems on p.Id equals pi.PrescriptionId
            select (decimal?)(pi.UnitPrice * pi.Dosage)
        ).SumAsync(x => x, cancellationToken) ?? 0m;
    }

    /// <inheritdoc/>
    public async Task<int> GetConsultationCountAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await _context.MedicalCases.CountAsync(
            mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value),
            cancellationToken
        );
    }

    /// <inheritdoc/>
    public async Task<List<DoctorCountDto>> GetConsultationsByDoctorAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
            )
            .GroupBy(mc => mc.DoctorName)
            .Select(g => new DoctorCountDto { DoctorName = g.Key, Count = g.Count() })
            .OrderByDescending(d => d.Count)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayValueDto>> GetRegistrationFeeByDayAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .Registrations.Where(r =>
                !r.IsDeleted && r.CreatedAt >= startDate && r.CreatedAt < endDate.AddDays(1)
                && (!doctorIdFilter.HasValue || r.DoctorId == doctorIdFilter.Value)
            )
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new ReportDayValueDto(g.Key, g.Sum(r => r.RegistrationFee)))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayValueDto>> GetMedicineFeeByDayAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await (
            from mc in _context.MedicalCases
            where
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
            join p in _context.Prescriptions on mc.Id equals p.MedicalCaseId
            where !p.IsDeleted
            join pi in _context.PrescriptionItems on p.Id equals pi.PrescriptionId
            group pi by mc.CreatedAt.Date into g
            select new ReportDayValueDto(g.Key, g.Sum(x => x.UnitPrice * x.Dosage))
        ).ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<ReportDayCountDto>> GetConsultationCountByDayAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        return await _context
            .MedicalCases.Where(mc =>
                !mc.IsDeleted
                && mc.CreatedAt >= startDate
                && mc.CreatedAt < endDate.AddDays(1)
                && mc.CaseStatus == MedicalCaseStatus.Completed
                && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
            )
            .GroupBy(mc => mc.CreatedAt.Date)
            .Select(g => new ReportDayCountDto(g.Key, g.Count()))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<List<DoctorPerformancePointDto>> GetDoctorPerformanceAsync(
        DateTime startDate,
        DateTime endDate,
        Guid? doctorIdFilter = null,
        CancellationToken cancellationToken = default
    )
    {
        var cases = _context.MedicalCases.Where(mc =>
            !mc.IsDeleted
            && mc.CreatedAt >= startDate
            && mc.CreatedAt < endDate.AddDays(1)
            && mc.CaseStatus == MedicalCaseStatus.Completed
            && (!doctorIdFilter.HasValue || mc.UserId == doctorIdFilter.Value)
        );

        // 单次查询（P1-2 2026-08-14）: 以医案为驱动按医生分组，各指标用相关子查询聚合（挂号费/药费/处方数）——
        // 一次 SQL 完成全部统计，避免 4 次独立查询；相关子查询无多表 fan-out 失真。
        // 注: 不用 LEFT JOIN 预聚合子查询——EF InMemory 对子查询聚合 + LEFT JOIN 外层 Sum 翻译缺陷
        //（Nullable must have value）；相关子查询模式 InMemory/SQL Server 均可翻译。
        var query =
            from mc in cases
            group mc by mc.DoctorName into g
            select new
            {
                DoctorName = g.Key,
                ConsultationCount = g.Count(),
                RegistrationFeeTotal = g.SelectMany(c =>
                        _context.Registrations.Where(r => r.MedicalCaseId == c.Id && !r.IsDeleted)
                    )
                    .Sum(r => (decimal?)r.RegistrationFee)
                    ?? 0m,
                MedicineFeeTotal = g.SelectMany(c =>
                        _context.Prescriptions.Where(p => p.MedicalCaseId == c.Id && !p.IsDeleted)
                    )
                    .SelectMany(p =>
                        _context.PrescriptionItems.Where(pi => pi.PrescriptionId == p.Id)
                    )
                    .Sum(pi => (decimal?)(pi.UnitPrice * pi.Dosage))
                    ?? 0m,
                PrescriptionCount = g.SelectMany(c =>
                        _context.Prescriptions.Where(p => p.MedicalCaseId == c.Id && !p.IsDeleted)
                    )
                    .Count(),
            };

        var result = await query.ToListAsync(cancellationToken);
        return result
            .Select(x => new DoctorPerformancePointDto(
                x.DoctorName,
                x.ConsultationCount,
                x.RegistrationFeeTotal,
                x.MedicineFeeTotal,
                x.PrescriptionCount
            ))
            .OrderByDescending(d => d.ConsultationCount)
            .ToList();
    }

}
