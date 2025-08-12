using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.ValueObjects;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// 病案聚合根Repository实现 - DDD模式
    /// </summary>
    public class MedicalCaseRepository : DomainRepositoryBase<MedicalCase>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(AppDbContext context, ILogger<MedicalCaseRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据患者ID获取病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetByPatientIdAsync(Guid patientId)
        {
            Logger.LogDebug("Getting medical cases by patient ID: {PatientId}", patientId);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.PatientId == patientId)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据患者ID获取活跃病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetActiveByPatientIdAsync(Guid patientId)
        {
            Logger.LogDebug("Getting active medical cases by patient ID: {PatientId}", patientId);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.PatientId == patientId && mc.Status == CaseStatus.Active)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据医生ID获取病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetByDoctorIdAsync(Guid doctorId)
        {
            Logger.LogDebug("Getting medical cases by doctor ID: {DoctorId}", doctorId);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.DoctorId == doctorId)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据日期范围获取病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            Logger.LogDebug("Getting medical cases by date range: {StartDate} - {EndDate}", startDate, endDate);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.CreatedAt >= startDate && mc.CreatedAt <= endDate)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据病案号获取病案
        /// </summary>
        public async Task<MedicalCase> GetByCaseNumberAsync(string caseNumber)
        {
            Logger.LogDebug("Getting medical case by case number: {CaseNumber}", caseNumber);
            
            if (string.IsNullOrWhiteSpace(caseNumber))
            {
                return null;
            }

            return await QueryAsNoTracking()
                .FirstOrDefaultAsync(mc => mc.CaseInfo.CaseNumber == caseNumber);
        }

        /// <summary>
        /// 根据病案类型获取病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetByCaseTypeAsync(CaseType caseType)
        {
            Logger.LogDebug("Getting medical cases by case type: {CaseType}", caseType);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.CaseInfo.CaseType == caseType)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取急诊病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetEmergencyCasesAsync()
        {
            Logger.LogDebug("Getting emergency medical cases");
            
            return await QueryAsNoTracking()
                .Where(mc => mc.CaseInfo.IsEmergency)
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 根据主诉查找病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetByChiefComplaintAsync(string complaint)
        {
            Logger.LogDebug("Getting medical cases by chief complaint: {Complaint}", complaint);
            
            if (string.IsNullOrWhiteSpace(complaint))
            {
                return new List<MedicalCase>();
            }

            return await QueryAsNoTracking()
                .Where(mc => mc.ChiefComplaint.Description.Contains(complaint))
                .OrderByDescending(mc => mc.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// 获取需要随访的病案记录
        /// </summary>
        public async Task<List<MedicalCase>> GetCasesNeedingFollowUpAsync()
        {
            Logger.LogDebug("Getting medical cases needing follow-up");
            
            var today = DateTime.Today;
            
            return await QueryAsNoTracking()
                .Where(mc => mc.FollowUpPlan.NextFollowUpDate <= today && 
                           mc.Status != CaseStatus.Completed)
                .OrderBy(mc => mc.FollowUpPlan.NextFollowUpDate)
                .ToListAsync();
        }

        /// <summary>
        /// 获取患者最近的病案
        /// </summary>
        public async Task<MedicalCase> GetLatestByPatientIdAsync(Guid patientId)
        {
            Logger.LogDebug("Getting latest medical case by patient ID: {PatientId}", patientId);
            
            return await QueryAsNoTracking()
                .Where(mc => mc.PatientId == patientId)
                .OrderByDescending(mc => mc.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 检查病案号是否已存在
        /// </summary>
        public async Task<bool> IsCaseNumberExistsAsync(string caseNumber, Guid? excludeCaseId = null)
        {
            Logger.LogDebug("Checking if case number exists: {CaseNumber}, excluding: {ExcludeId}", 
                caseNumber, excludeCaseId);
            
            if (string.IsNullOrWhiteSpace(caseNumber))
            {
                return false;
            }

            var query = QueryAsNoTracking().Where(mc => mc.CaseInfo.CaseNumber == caseNumber);
            
            if (excludeCaseId.HasValue)
            {
                query = query.Where(mc => mc.Id != excludeCaseId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 获取病案统计信息
        /// </summary>
        public async Task<MedicalCaseStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            Logger.LogDebug("Getting medical case statistics for date range: {StartDate} - {EndDate}", startDate, endDate);
            
            var query = QueryAsNoTracking();
            
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(mc => mc.CreatedAt >= startDate.Value && mc.CreatedAt <= endDate.Value);
            }
            
            var totalCount = await query.CountAsync();
            var activeCount = await query.CountAsync(mc => mc.Status == CaseStatus.Active);
            var completedCount = await query.CountAsync(mc => mc.Status == CaseStatus.Completed);
            var emergencyCount = await query.CountAsync(mc => mc.CaseInfo.IsEmergency);
            var needFollowUpCount = await query.CountAsync(mc => 
                mc.FollowUpPlan.NextFollowUpDate <= DateTime.Today && mc.Status != CaseStatus.Completed);
            
            return new MedicalCaseStatistics
            {
                TotalCases = totalCount,
                ActiveCases = activeCount,
                CompletedCases = completedCount,
                EmergencyCases = emergencyCount,
                CasesNeedingFollowUp = needFollowUpCount
            };
        }

        /// <summary>
        /// 获取病案类型统计
        /// </summary>
        public async Task<List<CaseTypeStatistics>> GetCaseTypeStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            Logger.LogDebug("Getting case type statistics for date range: {StartDate} - {EndDate}", startDate, endDate);
            
            var query = QueryAsNoTracking();
            
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(mc => mc.CreatedAt >= startDate.Value && mc.CreatedAt <= endDate.Value);
            }
            
            return await query
                .GroupBy(mc => mc.CaseInfo.CaseType)
                .Select(g => new CaseTypeStatistics
                {
                    CaseType = g.Key,
                    Count = g.Count(),
                    ActiveCount = g.Count(mc => mc.Status == CaseStatus.Active),
                    CompletedCount = g.Count(mc => mc.Status == CaseStatus.Completed)
                })
                .OrderByDescending(s => s.Count)
                .ToListAsync();
        }

        /// <summary>
        /// 包含导航属性的查询重写
        /// </summary>
        protected override IQueryable<MedicalCase> IncludeNavigationProperties(IQueryable<MedicalCase> query)
        {
            // 病案聚合根已经通过EF Core的OwnsOne和OwnsMany配置包含了所有值对象和子实体
            // 无需额外的Include，EF Core会自动加载拥有的实体
            return query;
        }
    }

    /// <summary>
    /// 病案统计信息
    /// </summary>
    public class MedicalCaseStatistics
    {
        public int TotalCases { get; set; }
        public int ActiveCases { get; set; }
        public int CompletedCases { get; set; }
        public int EmergencyCases { get; set; }
        public int CasesNeedingFollowUp { get; set; }
    }

    /// <summary>
    /// 病案类型统计
    /// </summary>
    public class CaseTypeStatistics
    {
        public CaseType CaseType { get; set; }
        public int Count { get; set; }
        public int ActiveCount { get; set; }
        public int CompletedCount { get; set; }
    }
}