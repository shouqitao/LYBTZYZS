using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Domain.SeedWork;
using LYBT.Domain.Common;
using LYBT.Domain.Aggregates.PatientAggregate;

namespace LYBT.Infrastructure.Repositories.DDD
{
    /// <summary>
    /// 患者聚合根Repository实现 - DDD模式
    /// </summary>
    public class PatientRepository : DomainRepositoryBase<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context, ILogger<PatientRepository> logger) 
            : base(context, logger)
        {
        }

        /// <summary>
        /// 根据电话号码查找患者
        /// </summary>
        public async Task<List<Patient>> GetByPhoneNumberAsync(string phoneNumber)
        {
            Logger.LogDebug("Getting patients by phone number: {PhoneNumber}", phoneNumber);
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return new List<Patient>();
            }

            return await QueryAsNoTracking()
                .Where(p => p.PhoneNumber == phoneNumber)
                .ToListAsync();
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<Patient> GetByIdNumberAsync(string idNumber)
        {
            Logger.LogDebug("Getting patient by ID number: {IdNumber}", idNumber);
            
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                return null;
            }

            return await QueryAsNoTracking()
                .FirstOrDefaultAsync(p => p.Identity.IdNumber == idNumber);
        }

        /// <summary>
        /// 根据姓名模糊查找患者
        /// </summary>
        public async Task<List<Patient>> GetByNameAsync(string name)
        {
            Logger.LogDebug("Getting patients by name: {Name}", name);
            
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<Patient>();
            }

            return await QueryAsNoTracking()
                .Where(p => p.Name.Contains(name) || 
                           p.PersonalInfo.PinYinCode.Contains(name.ToUpper()) ||
                           p.PersonalInfo.WuBiCode.Contains(name.ToUpper()))
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 获取活跃患者（状态正常的患者）
        /// </summary>
        public async Task<List<Patient>> GetActivePatients()
        {
            Logger.LogDebug("Getting active patients");
            
            return await QueryAsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        /// <summary>
        /// 检查电话号码是否已存在
        /// </summary>
        public async Task<bool> IsPhoneNumberExistsAsync(string phoneNumber, Guid? excludePatientId = null)
        {
            Logger.LogDebug("Checking if phone number exists: {PhoneNumber}, excluding: {ExcludeId}", 
                phoneNumber, excludePatientId);
            
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            var query = QueryAsNoTracking().Where(p => p.PhoneNumber == phoneNumber);
            
            if (excludePatientId.HasValue)
            {
                query = query.Where(p => p.Id != excludePatientId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 检查身份证号是否已存在
        /// </summary>
        public async Task<bool> IsIdNumberExistsAsync(string idNumber, Guid? excludePatientId = null)
        {
            Logger.LogDebug("Checking if ID number exists: {IdNumber}, excluding: {ExcludeId}", 
                idNumber, excludePatientId);
            
            if (string.IsNullOrWhiteSpace(idNumber))
            {
                return false;
            }

            var query = QueryAsNoTracking().Where(p => p.Identity.IdNumber == idNumber);
            
            if (excludePatientId.HasValue)
            {
                query = query.Where(p => p.Id != excludePatientId.Value);
            }

            return await query.AnyAsync();
        }

        /// <summary>
        /// 获取患者统计信息
        /// </summary>
        public async Task<PatientStatistics> GetStatisticsAsync()
        {
            Logger.LogDebug("Getting patient statistics");
            
            var totalCount = await CountAsync();
            var activeCount = await CountAsync(p => p.IsActive);
            var todayRegistered = await CountAsync(p => p.CreatedAt.Date == DateTime.Today);
            
            return new PatientStatistics
            {
                TotalPatients = totalCount,
                ActivePatients = activeCount,
                InactivePatients = totalCount - activeCount,
                TodayRegistered = todayRegistered
            };
        }

        /// <summary>
        /// 包含导航属性的查询重写
        /// </summary>
        protected override IQueryable<Patient> IncludeNavigationProperties(IQueryable<Patient> query)
        {
            // 患者聚合根已经通过EF Core的OwnsOne配置包含了所有值对象
            // 无需额外的Include，EF Core会自动加载拥有的实体
            return query;
        }
    }

    /// <summary>
    /// 患者统计信息
    /// </summary>
    public class PatientStatistics
    {
        public int TotalPatients { get; set; }
        public int ActivePatients { get; set; }
        public int InactivePatients { get; set; }
        public int TodayRegistered { get; set; }
    }
}