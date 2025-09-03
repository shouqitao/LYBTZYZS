using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者查询服务 - UltraThink架构
    /// 职责：所有查询、搜索和统计相关逻辑
    /// </summary>
    public class PatientQueryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientQueryService> _logger;

        public PatientQueryService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PatientQueryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分页查询患者记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                var patientsQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索 - 搜索姓名和手机号
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    patientsQuery = patientsQuery.Where(p =>
                        (p.Name != null && p.Name.Contains(query.Keyword)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(query.Keyword)) ||
                        (p.PinYinCode != null && p.PinYinCode.Contains(query.Keyword.ToUpper())));
                }

                // 排序 - 使用创建时间排序
                patientsQuery = patientsQuery.OrderByDescending(p => p.CreatedAt);

                var totalCount = await patientsQuery.CountAsync();
                var patients = await patientsQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var items = _mapper.Map<List<PatientDto>>(patients);

                var result = new PagedResult<PatientDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询患者记录失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure("分页查询患者记录失败", ex);
            }
        }

        /// <summary>
        /// 获取所有活跃患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetAllActiveAsync()
        {
            try
            {
                var patients = await _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var patientDtos = _mapper.Map<List<PatientDto>>(patients);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有活跃患者失败");
                return ServiceResult<List<PatientDto>>.Failure("获取所有活跃患者失败", ex);
            }
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                    return ServiceResult<PatientDto>.Failure("手机号不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber && p.Status == CommonStatus.Enabled);

                if (patient == null)
                    return ServiceResult<PatientDto>.Failure("未找到患者");

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据手机号查找患者失败: {PhoneNumber}", phoneNumber);
                return ServiceResult<PatientDto>.Failure("根据手机号查找患者失败", ex);
            }
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdNumberAsync(string idNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idNumber))
                    return ServiceResult<PatientDto>.Failure("身份证号不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdNumber == idNumber && p.Status == CommonStatus.Enabled);

                if (patient == null)
                    return ServiceResult<PatientDto>.Failure("未找到患者");

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号查找患者失败: {IdNumber}", idNumber);
                return ServiceResult<PatientDto>.Failure("根据身份证号查找患者失败", ex);
            }
        }

        /// <summary>
        /// 搜索患者记录
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

                var patients = await _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled && (
                        (p.Name != null && p.Name.Contains(keyword)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(keyword)) ||
                        (p.IdNumber != null && p.IdNumber.Contains(keyword)) ||
                        (p.PinYinCode != null && p.PinYinCode.Contains(keyword.ToUpper()))
                    ))
                    .OrderBy(p => p.Name)
                    .Take(20)
                    .ToListAsync();

                var patientDtos = _mapper.Map<List<PatientDto>>(patients);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者记录失败: {Keyword}", keyword);
                return ServiceResult<List<PatientDto>>.Failure("搜索患者记录失败", ex);
            }
        }

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            try
            {
                var patientsQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 姓名搜索
                if (!string.IsNullOrWhiteSpace(query.Name))
                {
                    patientsQuery = patientsQuery.Where(p => p.Name != null && p.Name.Contains(query.Name));
                }

                // 手机号搜索
                if (!string.IsNullOrWhiteSpace(query.PhoneNumber))
                {
                    patientsQuery = patientsQuery.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(query.PhoneNumber));
                }

                // 身份证号搜索
                if (!string.IsNullOrWhiteSpace(query.IdCardNumber))
                {
                    patientsQuery = patientsQuery.Where(p => p.IdNumber != null && p.IdNumber.Contains(query.IdCardNumber));
                }

                // 年龄范围搜索
                if (query.MinAge.HasValue || query.MaxAge.HasValue)
                {
                    var today = DateTime.Today;
                    
                    if (query.MinAge.HasValue)
                    {
                        var maxBirthDate = today.AddYears(-query.MinAge.Value);
                        patientsQuery = patientsQuery.Where(p => p.BirthDate <= maxBirthDate);
                    }
                    
                    if (query.MaxAge.HasValue)
                    {
                        var minBirthDate = today.AddYears(-query.MaxAge.Value - 1);
                        patientsQuery = patientsQuery.Where(p => p.BirthDate >= minBirthDate);
                    }
                }

                // 性别搜索
                if (query.Gender.HasValue)
                {
                    patientsQuery = patientsQuery.Where(p => p.Gender == query.Gender.Value);
                }

                // 排序
                patientsQuery = patientsQuery.OrderByDescending(p => p.CreatedAt);

                var totalCount = await patientsQuery.CountAsync();
                var patients = await patientsQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var items = _mapper.Map<List<PatientDto>>(patients);

                var result = new PagedResult<PatientDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高级搜索患者失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure("高级搜索患者失败", ex);
            }
        }

        /// <summary>
        /// 检查重复患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            try
            {
                var duplicatesQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                var conditions = new List<System.Linq.Expressions.Expression<Func<Patient, bool>>>();

                if (!string.IsNullOrWhiteSpace(idNumber))
                {
                    conditions.Add(p => p.IdNumber == idNumber);
                }

                if (!string.IsNullOrWhiteSpace(phoneNumber))
                {
                    conditions.Add(p => p.PhoneNumber == phoneNumber);
                }

                if (!conditions.Any())
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());

                // 合并查询条件
                var combinedCondition = conditions.Aggregate((acc, condition) => 
                    Expression.Lambda<Func<Patient, bool>>(
                        Expression.OrElse(acc.Body, condition.Body),
                        acc.Parameters));

                var duplicatePatients = await duplicatesQuery
                    .Where(combinedCondition)
                    .ToListAsync();

                var duplicateDtos = _mapper.Map<List<PatientDto>>(duplicatePatients);
                return ServiceResult<List<PatientDto>>.Success(duplicateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查重复患者失败");
                return ServiceResult<List<PatientDto>>.Failure("检查重复患者失败", ex);
            }
        }

        /// <summary>
        /// 获取患者基本统计信息
        /// </summary>
        public async Task<ServiceResult<object>> GetBasicStatisticsAsync()
        {
            try
            {
                var totalCount = await _context.Patients.CountAsync(p => p.Status == CommonStatus.Enabled);
                var maleCount = await _context.Patients.CountAsync(p => p.Status == CommonStatus.Enabled && p.Gender == Gender.Male);
                var femaleCount = await _context.Patients.CountAsync(p => p.Status == CommonStatus.Enabled && p.Gender == Gender.Female);

                var statistics = new
                {
                    TotalPatients = totalCount,
                    MalePatients = maleCount,
                    FemalePatients = femaleCount
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取基本统计信息失败");
                return ServiceResult<object>.Failure("获取基本统计信息失败", ex);
            }
        }
    }
}