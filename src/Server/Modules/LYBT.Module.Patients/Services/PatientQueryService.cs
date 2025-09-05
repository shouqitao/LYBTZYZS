using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者查询服务实现
    /// UltraThink架构 - Query层接口抽象
    /// 职责：患者查询、搜索、统计功能专业化处理
    /// </summary>
    public class PatientQueryService : IPatientQueryService
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
        /// 分页查询患者列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var patientsQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    patientsQuery = patientsQuery.Where(p =>
                        (p.Name != null && p.Name.Contains(query.Keyword)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(query.Keyword)) ||
                        (p.PinYinCode != null && p.PinYinCode.Contains(query.Keyword.ToUpper())));
                }

                var totalCount = await patientsQuery.CountAsync();
                var patients = await patientsQuery
                    .OrderByDescending(p => p.CreatedAt)
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
                _logger.LogError(ex, "分页查询患者列表失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure($"分页查询患者列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取患者详情失败: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure($"根据ID获取患者详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetAllAsync()
        {
            try
            {
                var patients = await _context.Patients
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var patientDtos = _mapper.Map<List<PatientDto>>(patients);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有患者列表失败");
                return ServiceResult<List<PatientDto>>.Failure($"获取所有患者列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取活跃患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetActivePatientsAsync()
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
                _logger.LogError(ex, "获取活跃患者列表失败");
                return ServiceResult<List<PatientDto>>.Failure($"获取活跃患者列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据身份证号查询患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIDNumberAsync(string idNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idNumber))
                {
                    return ServiceResult<PatientDto>.Failure("身份证号不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdNumber == idNumber);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号查询患者失败: {IdNumber}", idNumber);
                return ServiceResult<PatientDto>.Failure($"根据身份证号查询患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据手机号查询患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    return ServiceResult<PatientDto>.Failure("手机号码不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PhoneNumber == phoneNumber);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据手机号查询患者失败: {PhoneNumber}", phoneNumber);
                return ServiceResult<PatientDto>.Failure($"根据手机号查询患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据身份证号获取患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(idCard))
                {
                    return ServiceResult<PatientDto>.Failure("身份证号不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdNumber == idCard);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                var patientDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(patientDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号获取患者失败: {IdCard}", idCard);
                return ServiceResult<PatientDto>.Failure($"根据身份证号获取患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据手机号获取患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return ServiceResult<List<PatientDto>>.Failure("手机号码不能为空");
                }

                var patients = await _context.Patients
                    .Where(p => p.PhoneNumber.Contains(phone))
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var patientDtos = _mapper.Map<List<PatientDto>>(patients);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据手机号获取患者列表失败: {Phone}", phone);
                return ServiceResult<List<PatientDto>>.Failure($"根据手机号获取患者列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
                }

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
                _logger.LogError(ex, "搜索患者失败: {Keyword}", keyword);
                return ServiceResult<List<PatientDto>>.Failure($"搜索患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 高级搜索患者
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> AdvancedSearchAsync(PatientSearchDto searchDto)
        {
            try
            {
                var patientsQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索
                if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
                {
                    patientsQuery = patientsQuery.Where(p =>
                        (p.Name != null && p.Name.Contains(searchDto.Keyword)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(searchDto.Keyword)) ||
                        (p.IdNumber != null && p.IdNumber.Contains(searchDto.Keyword)));
                }

                // 姓名搜索
                if (!string.IsNullOrWhiteSpace(searchDto.Name))
                {
                    patientsQuery = patientsQuery.Where(p => p.Name != null && p.Name.Contains(searchDto.Name));
                }

                // 手机号搜索
                if (!string.IsNullOrWhiteSpace(searchDto.PhoneNumber))
                {
                    patientsQuery = patientsQuery.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(searchDto.PhoneNumber));
                }

                // 年龄范围搜索
                if (searchDto.MinAge.HasValue || searchDto.MaxAge.HasValue)
                {
                    var today = DateTime.Today;

                    if (searchDto.MinAge.HasValue)
                    {
                        var maxBirthDate = today.AddYears(-searchDto.MinAge.Value);
                        patientsQuery = patientsQuery.Where(p => p.BirthDate <= maxBirthDate);
                    }

                    if (searchDto.MaxAge.HasValue)
                    {
                        var minBirthDate = today.AddYears(-searchDto.MaxAge.Value - 1);
                        patientsQuery = patientsQuery.Where(p => p.BirthDate >= minBirthDate);
                    }
                }

                // 性别搜索
                if (searchDto.Gender.HasValue)
                {
                    patientsQuery = patientsQuery.Where(p => p.Gender == searchDto.Gender.Value);
                }

                var totalCount = await patientsQuery.CountAsync();
                var patients = await patientsQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((searchDto.PageIndex - 1) * searchDto.PageSize)
                    .Take(searchDto.PageSize)
                    .ToListAsync();

                var items = _mapper.Map<List<PatientDto>>(patients);

                var result = new PagedResult<PatientDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = searchDto.PageIndex,
                    PageSize = searchDto.PageSize
                };

                return ServiceResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高级搜索患者失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure($"高级搜索患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查重复患者
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> CheckDuplicatePatientsAsync(PatientCreateDto createDto)
        {
            try
            {
                var duplicatesQuery = _context.Patients.AsQueryable();
                var hasDuplicateCondition = false;

                // 检查手机号重复
                if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    duplicatesQuery = duplicatesQuery.Where(p => p.PhoneNumber == createDto.PhoneNumber);
                    hasDuplicateCondition = true;
                }

                // 检查身份证号重复
                if (!string.IsNullOrWhiteSpace(createDto.IdNumber))
                {
                    if (hasDuplicateCondition)
                    {
                        duplicatesQuery = _context.Patients
                            .Where(p => p.PhoneNumber == createDto.PhoneNumber || p.IdNumber == createDto.IdNumber);
                    }
                    else
                    {
                        duplicatesQuery = duplicatesQuery.Where(p => p.IdNumber == createDto.IdNumber);
                        hasDuplicateCondition = true;
                    }
                }

                if (!hasDuplicateCondition)
                {
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
                }

                var duplicatePatients = await duplicatesQuery.ToListAsync();
                var duplicateDtos = _mapper.Map<List<PatientDto>>(duplicatePatients);

                return ServiceResult<List<PatientDto>>.Success(duplicateDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查重复患者失败");
                return ServiceResult<List<PatientDto>>.Failure($"检查重复患者失败: {ex.Message}");
            }
        }
    }
}
