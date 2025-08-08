using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Models.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务实现
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly LYBT.Infrastructure.Data.AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            LYBT.Infrastructure.Data.AppDbContext dbContext,
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseService> logger)
        {
            _dbContext = dbContext;
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

        /// <summary>
        /// 获取所有医疗案例
        /// </summary>
        public async Task<List<MedicalCaseDto>> GetAllAsync()
        {
            var models = await _repository.GetListAsync();
            return _mapper.Map<List<MedicalCaseDto>>(models);
        }

        public async Task<PaginatedResult<MedicalCaseDto>> GetPagedAsync(PaginationRequest request)
        {
            try
            {
                var models = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);

                // 搜索过滤
                if (!string.IsNullOrWhiteSpace(request.SearchKeyword))
                {
                    dtos = dtos.Where(x =>
                        x.PatientName.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        x.DoctorName.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase) ||
                        x.DiagnosisSummary.Contains(request.SearchKeyword, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                // 排序 - 默认按创建时间降序
                dtos = dtos.OrderByDescending(x => x.CreateTime).ToList();

                // 分页
                var total = dtos.Count;
                var items = dtos
                    .Skip((request.CurrentPage - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToList();

                return new PaginatedResult<MedicalCaseDto>
                {
                    Items = items,
                    TotalCount = total,
                    CurrentPage = request.CurrentPage,
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
                model.Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Registered;
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
                    model.Status = (LYBT.Shared.Models.Enums.MedicalCaseStatus)dto.Status.Value;
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

        /// <summary>
        /// 更新医疗案例（使用EditDto）
        /// </summary>
        public async Task<bool> UpdateAsync(MedicalCaseEditDto dto)
        {
            return await UpdateAsync(dto.Id, dto);
        }

        public async Task<bool> UpdateStatusAsync(Guid id, LYBT.Shared.Models.Enums.MedicalCaseStatus status)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                {
                    _logger.LogWarning("医疗案例不存在，ID: {Id}", id);
                    return false;
                }

                model.Status = (LYBT.Shared.Models.Enums.MedicalCaseStatus)status;
                model.UpdateTime = DateTime.Now;

                if (status == LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed)
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
        public async Task<List<MedicalCaseDto>> GetByUserIdAsync(Guid userId)
        {
            try
            {
                var models = await _repository.GetByUserIdAsync(userId);
                return _mapper.Map<List<MedicalCaseDto>>(models);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据用户ID获取医疗案例列表失败，UserId: {UserId}", userId);
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
            return await UpdateStatusAsync(id, LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);
        }

        /// <summary>
        /// 获取今日医生的案例
        /// </summary>
        public async Task<List<MedicalCaseModel>> GetTodayByUserIdAsync(Guid userId)
        {
            var today = DateTime.Today;
            return await _dbContext.MedicalCases
                .Where(m => m.UserId == userId &&
                           m.CreateTime >= today &&
                           m.CreateTime < today.AddDays(1))
                .ToListAsync();
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<bool> StartConsultationAsync(Guid caseId, Guid consultationId)
        {
            var medicalCase = await _dbContext.MedicalCases.FindAsync(caseId);
            if (medicalCase == null) return false;

            medicalCase.ConsultationId = consultationId;
            medicalCase.Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.InConsultation;
            medicalCase.UpdateTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<bool> CompleteConsultationAsync(Guid caseId, Guid? prescriptionId)
        {
            var medicalCase = await _dbContext.MedicalCases.FindAsync(caseId);
            if (medicalCase == null) return false;

            medicalCase.Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed;
            medicalCase.UpdateTime = DateTime.Now;
            medicalCase.CompleteTime = DateTime.Now;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 完成医疗案例
        /// </summary>
        public async Task<bool> CompleteMedicalCaseAsync(Guid id)
        {
            return await UpdateStatusAsync(id, LYBT.Shared.Models.Enums.MedicalCaseStatus.Completed);
        }

        /// <summary>
        /// 取消医疗案例
        /// </summary>
        public async Task<bool> CancelMedicalCaseAsync(Guid id, string reason)
        {
            var medicalCase = await _dbContext.MedicalCases.FindAsync(id);
            if (medicalCase == null) return false;

            medicalCase.Status = LYBT.Shared.Models.Enums.MedicalCaseStatus.Cancelled;
            medicalCase.UpdateTime = DateTime.Now;
            medicalCase.Remark = reason;

            await _dbContext.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// 根据状态获取待处理案例
        /// </summary>
        public async Task<List<MedicalCaseModel>> GetPendingCasesByStatusAsync(LYBT.Shared.Models.Enums.MedicalCaseStatus status)
        {
            return await _dbContext.MedicalCases
                .Where(m => m.Status == status && m.IsActive)
                .OrderBy(m => m.CreateTime)
                .ToListAsync();
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        public async Task<(List<MedicalCaseModel> Items, int Total)> GetPagedAsync(
            int pageIndex,
            int pageSize,
            LYBT.Shared.Models.Enums.MedicalCaseStatus? status = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var query = _dbContext.MedicalCases.AsQueryable();

            if (status.HasValue)
            {
                query = query.Where(m => m.Status == status.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(m => m.CreateTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(m => m.CreateTime <= endDate.Value);
            }

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(m => m.CreateTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
