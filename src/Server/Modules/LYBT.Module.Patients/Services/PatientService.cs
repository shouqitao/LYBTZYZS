using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class PatientService : Interfaces.IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientService> _logger;

        public PatientService(
            IPatientRepository repository,
            IMapper mapper,
            ILogger<PatientService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dto = new PagedResult<PatientDto>
                {
                    Items = _mapper.Map<List<PatientDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<PatientDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者列表失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure("获取患者列表失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                var dto = _mapper.Map<PatientDto>(entity);
                return ServiceResult<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败");
                return ServiceResult<PatientDto>.Failure("获取患者详情失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<Patient>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建患者失败");
                return ServiceResult<PatientDto>.Failure("创建患者失败");
            }
        }

        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<PatientDto>(result);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败");
                return ServiceResult<PatientDto>.Failure("更新患者失败");
            }
        }

        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 如果关键字为空，返回空列表
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<PatientDto>>.Success(new List<PatientDto>());
                }

                // 搜索匹配关键字的患者（姓名、电话或身份证号）
                var allPatients = await _repository.GetAllAsync();
                var patients = allPatients.Where(p =>
                    p.Name.Contains(keyword)).ToList();

                // 转换为DTO
                var patientDtos = _mapper.Map<List<PatientDto>>(patients);

                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者时发生错误，关键字：{Keyword}", keyword);
                return ServiceResult<List<PatientDto>>.Failure($"搜索患者失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败");
                return ServiceResult.Failure("删除患者失败");
            }
        }
    }
}
