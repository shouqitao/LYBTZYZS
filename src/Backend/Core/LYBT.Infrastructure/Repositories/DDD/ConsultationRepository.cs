using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Aggregates.ConsultationAggregate;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// 看诊聚合根Repository实现 - DDD模式
    /// </summary>
    public class ConsultationRepository : DomainRepositoryBase<Consultation>, IConsultationRepository
    {
        public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据患者ID获取看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetByPatientIdAsync(Guid patientId)
        {
            Logger.LogDebug("Getting consultations by patient ID: {PatientId}", patientId);
            
            return await QueryAsNoTracking()
                .Where(c => c.PatientId == patientId)
                .OrderByDescending(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetByDoctorIdAsync(Guid doctorId)
        {
            Logger.LogDebug("Getting consultations by doctor ID: {DoctorId}", doctorId);
            
            return await QueryAsNoTracking()
                .Where(c => c.DoctorId == doctorId)
                .OrderByDescending(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据日期范围获取看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            Logger.LogDebug("Getting consultations by date range: {StartDate} - {EndDate}", startDate, endDate);
            
            return await QueryAsNoTracking()
                .Where(c => c.ConsultationTime >= startDate && c.ConsultationTime <= endDate)
                .OrderByDescending(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取患者的最近看诊记录
        /// </summary>
        public async Task<Consultation> GetLatestByPatientIdAsync(Guid patientId)
        {
            Logger.LogDebug("Getting latest consultation by patient ID: {PatientId}", patientId);
            
            return await QueryAsNoTracking()
                .Where(c => c.PatientId == patientId)
                .OrderByDescending(c => c.ConsultationTime)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// 获取医生今日的看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetTodayConsultationsByDoctorAsync(Guid doctorId)
        {
            Logger.LogDebug("Getting today's consultations by doctor ID: {DoctorId}", doctorId);
            
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            
            return await QueryAsNoTracking()
                .Where(c => c.DoctorId == doctorId && 
                           c.ConsultationTime >= today && 
                           c.ConsultationTime < tomorrow)
                .OrderBy(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据TCM诊断查找看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetByTCMDiagnosisAsync(string tcmDiagnosis)
        {
            Logger.LogDebug("Getting consultations by TCM diagnosis: {TCMDiagnosis}", tcmDiagnosis);
            
            if (string.IsNullOrWhiteSpace(tcmDiagnosis))
            {
                return new List<Consultation>();
            }

            return await QueryAsNoTracking()
                .Where(c => c.Diagnosis.TCMDiagnosis.Contains(tcmDiagnosis))
                .OrderByDescending(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 根据西医诊断查找看诊记录
        /// </summary>
        public async Task<List<Consultation>> GetByWesternDiagnosisAsync(string westernDiagnosis)
        {
            Logger.LogDebug("Getting consultations by Western diagnosis: {WesternDiagnosis}", westernDiagnosis);
            
            if (string.IsNullOrWhiteSpace(westernDiagnosis))
            {
                return new List<Consultation>();
            }

            return await QueryAsNoTracking()
                .Where(c => c.Diagnosis.WesternDiagnosis.Contains(westernDiagnosis))
                .OrderByDescending(c => c.ConsultationTime)
                .ToListAsync();
        }

        /// <summary>
        /// 获取看诊统计信息
        /// </summary>
        public async Task<ConsultationStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            Logger.LogDebug("Getting consultation statistics for date range: {StartDate} - {EndDate}", startDate, endDate);
            
            var query = QueryAsNoTracking();
            
            if (startDate.HasValue && endDate.HasValue)
            {
                query = query.Where(c => c.ConsultationTime >= startDate.Value && c.ConsultationTime <= endDate.Value);
            }
            
            var totalCount = await query.CountAsync();
            var todayCount = await query.CountAsync(c => c.ConsultationTime.Date == DateTime.Today);
            var completedCount = await query.CountAsync(c => c.Status == ConsultationStatus.Completed);
            
            return new ConsultationStatistics
            {
                TotalConsultations = totalCount,
                TodayConsultations = todayCount,
                CompletedConsultations = completedCount,
                InProgressConsultations = totalCount - completedCount
            };
        }

        /// <summary>
        /// 获取医生的工作量统计
        /// </summary>
        public async Task<List<DoctorWorkload>> GetDoctorWorkloadAsync(DateTime startDate, DateTime endDate)
        {
            Logger.LogDebug("Getting doctor workload for date range: {StartDate} - {EndDate}", startDate, endDate);
            
            return await QueryAsNoTracking()
                .Where(c => c.ConsultationTime >= startDate && c.ConsultationTime <= endDate)
                .GroupBy(c => new { c.DoctorId, c.DoctorName })
                .Select(g => new DoctorWorkload
                {
                    DoctorId = g.Key.DoctorId,
                    DoctorName = g.Key.DoctorName,
                    ConsultationCount = g.Count(),
                    CompletedCount = g.Count(c => c.Status == ConsultationStatus.Completed)
                })
                .OrderByDescending(d => d.ConsultationCount)
                .ToListAsync();
        }

        /// <summary>
        /// 包含导航属性的查询重写
        /// </summary>
        protected override IQueryable<Consultation> IncludeNavigationProperties(IQueryable<Consultation> query)
        {
            // 看诊聚合根已经通过EF Core的OwnsOne配置包含了所有值对象
            // 无需额外的Include，EF Core会自动加载拥有的实体
            return query;
        }
    }

    /// <summary>
    /// 看诊统计信息
    /// </summary>
    public class ConsultationStatistics
    {
        public int TotalConsultations { get; set; }
        public int TodayConsultations { get; set; }
        public int CompletedConsultations { get; set; }
        public int InProgressConsultations { get; set; }
    }

    /// <summary>
    /// 医生工作量统计
    /// </summary>
    public class DoctorWorkload
    {
        public Guid DoctorId { get; set; }
        public string DoctorName { get; set; }
        public int ConsultationCount { get; set; }
        public int CompletedCount { get; set; }
    }
}