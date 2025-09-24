using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{

    /// <summary>
    /// 患者查询服务实现
    /// UltraThink架构 - Query层接口抽象
    /// 职责：患者查询、搜索、统计功能专业化处理
    /// 改为使用ReadRepository，移除直接的DbContext依赖
    /// </summary>
    public class PatientQueryService : IPatientQueryService
    {
        private readonly IPatientReadRepository _readRepository;
        private readonly ILogger<PatientQueryService> _logger;

        public PatientQueryService(
            IPatientReadRepository readRepository,
            ILogger<PatientQueryService> logger)
        {
            _readRepository = readRepository ?? throw new ArgumentNullException(nameof(readRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分页查询患者列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var pageIndex = Math.Max(query.PageIndex, 1);
                var pageSize = Math.Clamp(query.PageSize, 10, 100);

                var queryDto = new PatientQueryDto
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize,
                    Keyword = query.Keyword
                };

                var pagedResult = await _readRepository.GetPagedPatientDtosAsync(queryDto);

                return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
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

                var dto = await _readRepository.GetPatientDtoByIdAsync(patientId);
                if (dto == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                return ServiceResult<PatientDto>.Success(dto);
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
                var dtos = await _readRepository.GetAllPatientDtosAsync();
                return ServiceResult<List<PatientDto>>.Success(dtos);
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
                var dtos = await _readRepository.GetActivePatientDtosAsync();
                return ServiceResult<List<PatientDto>>.Success(dtos);
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

                var dto = await _readRepository.GetPatientDtoByIdNumberAsync(idNumber);
                if (dto == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                return ServiceResult<PatientDto>.Success(dto);
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

                var dto = await _readRepository.GetPatientDtoByPhoneNumberAsync(phoneNumber);
                if (dto == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                return ServiceResult<PatientDto>.Success(dto);
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

                var dto = await _readRepository.GetPatientDtoByIdNumberAsync(idCard);
                if (dto == null)
                {
                    return ServiceResult<PatientDto>.Failure("未找到患者");
                }

                return ServiceResult<PatientDto>.Success(dto);
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

                var dtos = await _readRepository.GetPatientDtosByPhoneAsync(phone);
                return ServiceResult<List<PatientDto>>.Success(dtos);
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

                var dtos = await _readRepository.SearchPatientDtosAsync(keyword, 20);
                return ServiceResult<List<PatientDto>>.Success(dtos);
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
                var pageIndex = Math.Max(searchDto.PageIndex, 1);
                var pageSize = Math.Clamp(searchDto.PageSize, 10, 100);

                var pagedResult = await _readRepository.AdvancedSearchPatientDtosAsync(searchDto);
                return ServiceResult<PagedResult<PatientDto>>.Success(pagedResult);
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
                var dtos = await _readRepository.CheckDuplicatePatientDtosAsync(createDto);
                return ServiceResult<List<PatientDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查重复患者失败");
                return ServiceResult<List<PatientDto>>.Failure($"检查重复患者失败: {ex.Message}");
            }
        }
    }
}
