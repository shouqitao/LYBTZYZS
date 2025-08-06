using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Models.MedicalCase;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务实现
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetListAsync()
        {
            try
            {
                var models = await _repository.GetListAsync();
                return _mapper.Map<List<MedicalCaseDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例列表失败");
                throw;
            }
        }

        /// <summary>
        /// 分页获取医疗案例列表
        /// </summary>
        public async Task<PaginatedResult<MedicalCaseDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var models = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    dtos = dtos.Where(x => 
                        x.PatientName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.DoctorName.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                        x.DiagnosisSummary.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 排序
                dtos = request.SortBy?.ToLower() switch
                {
                    "patientname" => request.SortDesc ? dtos.OrderByDescending(x => x.PatientName).ToList() : dtos.OrderBy(x => x.PatientName).ToList(),
                    "doctorname" => request.SortDesc ? dtos.OrderByDescending(x => x.DoctorName).ToList() : dtos.OrderBy(x => x.DoctorName).ToList(),
                    "createtime" => request.SortDesc ? dtos.OrderByDescending(x => x.CreateTime).ToList() : dtos.OrderBy(x => x.CreateTime).ToList(),
                    _ => dtos.OrderByDescending(x => x.CreateTime).ToList()
                };

                // 分页
                var total = dtos.Count;
                var items = dtos
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new PaginatedResult<MedicalCaseDto>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取医疗案例列表失败");
                throw;
            }
        }

        /// <summary>
        /// 获取医疗案例详情
        /// </summary>
        public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                return model == null ? null : _mapper.Map<MedicalCaseDetailDto>(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<MedicalCaseModel>(dto);
                model.Id = Guid.NewGuid();
                model.CreateTime = DateTime.Now;
                model.Status = LYBT.Models.MedicalCase.MedicalCaseStatus.Registered;
                model.IsActive = true;

                var created = await _repository.CreateAsync(model);
                return _mapper.Map<MedicalCaseDetailDto>(created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                throw;
            }
        }

        /// <summary>
        /// 更新医疗案例
        /// </summary>
        public async Task<bool> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("医疗案例不存在，ID: {Id}", id);
                    return false;
                }

                // 更新字段
                if (dto.Status.HasValue)
                    model.Status = dto.Status.Value;
                if (!string.IsNullOrWhiteSpace(dto.Remark))
                    model.Remark = dto.Remark;
                if (dto.CompleteTime.HasValue)
                    model.CompleteTime = dto.CompleteTime.Value;

                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        public async Task<bool> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("医疗案例不存在，ID: {Id}", id);
                    return false;
                }

                model.Status = status;
                model.UpdateTime = DateTime.Now;

                if (status == LYBT.Models.MedicalCase.MedicalCaseStatus.Completed)
                {
                    model.CompleteTime = DateTime.Now;
                }

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败，ID: {Id}, Status: {Status}", id, status);
                throw;
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("医疗案例不存在，ID: {Id}", id);
                    return false;
                }

                model.IsActive = false;
                model.UpdateTime = DateTime.Now;

                return await _repository.UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var models = await _repository.GetByPatientIdAsync(patientId);
                return _mapper.Map<List<MedicalCaseDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例列表失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 根据医生ID获取医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                var models = await _repository.GetByDoctorIdAsync(doctorId);
                return _mapper.Map<List<MedicalCaseDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取医疗案例列表失败，DoctorId: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取今日医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetTodayCasesAsync()
        {
            try
            {
                var today = DateTime.Today;
                var models = await _repository.GetByDateRangeAsync(today, today.AddDays(1));
                return _mapper.Map<List<MedicalCaseDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取今日医疗案例列表失败");
                throw;
            }
        }

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        public async Task<bool> CompleteCaseAsync(Guid id)
        {
            return await UpdateStatusAsync(id, LYBT.Models.MedicalCase.MedicalCaseStatus.Completed);
        }
    }
}
}