using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using LYBT.Module.MedicalCase.Interfaces;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务实现 - 重构为Shared接口标准
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly LYBT.Infrastructure.Data.AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            LYBT.Infrastructure.Data.AppDbContext context,
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseService> logger)
        {
            _context = context;
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 根据ID获取医疗案例详情 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例不存在");

                var detailDto = _mapper.Map<MedicalCaseDetailDto>(model);
                return ServiceResult<MedicalCaseDetailDto>.Success(detailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure("获取医疗案例详情失败", ex);
            }
        }

        /// <summary>
        /// 分页查询医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var dbQuery = _context.MedicalCases
                    .Include(x => x.Consultation)
                    .Where(x => x.IsActive)
                    .AsQueryable();

                // 关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    dbQuery = dbQuery.Where(x =>
                        x.Id.ToString().Contains(query.Keyword) ||
                        x.PatientId.ToString().Contains(query.Keyword) ||
                        x.UserId.ToString().Contains(query.Keyword) ||
                        (x.Remark != null && x.Remark.Contains(query.Keyword))
                    );
                }

                // 排序
                dbQuery = query.IsDescending
                    ? dbQuery.OrderByDescending(x => x.CreateTime)
                    : dbQuery.OrderBy(x => x.CreateTime);

                // 获取总数
                var totalCount = await dbQuery.CountAsync();

                // 分页
                var items = await dbQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // 映射到DTO
                var dtos = _mapper.Map<List<MedicalCaseDto>>(items);

                var result = new PagedResult<MedicalCaseDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("分页查询医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 创建新的医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                var model = _mapper.Map<MedicalCaseModel>(dto);
                model.Id = Guid.NewGuid();
                model.CreateTime = DateTime.Now;
                model.Status = MedicalCaseStatus.Registered;
                model.IsActive = true;

                var created = await _repository.AddAsync(model);
                var createdDto = _mapper.Map<MedicalCaseDto>(created);

                return ServiceResult<MedicalCaseDto>.Success(createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 更新医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

                // 更新字段
                if (!string.IsNullOrWhiteSpace(dto.Status))
                {
                    if (Enum.TryParse<MedicalCaseStatus>(dto.Status, out var status))
                        model.Status = status;
                }
                if (!string.IsNullOrWhiteSpace(dto.Remark))
                    model.Remark = dto.Remark;
                if (dto.CompleteTime.HasValue)
                    model.CompleteTime = dto.CompleteTime.Value;

                model.UpdateTime = DateTime.Now;

                var updated = await _repository.UpdateAsync(model);
                if (updated == null)
                    return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");

                var updatedDto = _mapper.Map<MedicalCaseDto>(updated);
                return ServiceResult<MedicalCaseDto>.Success(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 删除医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                model.IsActive = false;
                model.UpdateTime = DateTime.Now;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var models = await _repository.GetByPatientIdAsync(patientId);
                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure("获取患者医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 获取患者的活跃医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                var activeCase = await _context.MedicalCases
                    .Where(m => m.PatientId == patientId && 
                               m.IsActive && 
                               (m.Status == MedicalCaseStatus.Registered || 
                                m.Status == MedicalCaseStatus.InConsultation))
                    .OrderByDescending(m => m.CreateTime)
                    .FirstOrDefaultAsync();

                if (activeCase == null)
                    return ServiceResult<MedicalCaseDto>.Failure("患者没有活跃的医疗案例");

                var dto = _mapper.Map<MedicalCaseDto>(activeCase);
                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure("获取患者活跃医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 完成医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                model.Status = MedicalCaseStatus.Completed;
                model.CompleteTime = DateTime.Now;
                model.UpdateTime = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(completionReason))
                    model.Remark = completionReason;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("完成医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 暂停医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                model.Status = MedicalCaseStatus.Suspended;
                model.UpdateTime = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(reason))
                    model.Remark = reason;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("暂停医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 恢复医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                model.Status = MedicalCaseStatus.InConsultation;
                model.UpdateTime = DateTime.Now;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("恢复医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 归档医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                model.Status = MedicalCaseStatus.Archived;
                model.UpdateTime = DateTime.Now;
                if (!string.IsNullOrWhiteSpace(archiveReason))
                    model.Remark = archiveReason;

                var result = await _repository.UpdateAsync(model);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure("归档医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 获取医疗案例统计信息 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.MedicalCases
                    .Where(m => m.IsActive);

                if (startDate.HasValue)
                    query = query.Where(m => m.CreateTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(m => m.CreateTime <= endDate.Value);

                var totalCount = await query.CountAsync();
                var completedCount = await query.Where(m => m.Status == MedicalCaseStatus.Completed).CountAsync();
                var inProgressCount = await query.Where(m => m.Status == MedicalCaseStatus.InConsultation).CountAsync();
                var pendingCount = await query.Where(m => m.Status == MedicalCaseStatus.Registered).CountAsync();

                var statistics = new
                {
                    TotalCases = totalCount,
                    CompletedCases = completedCount,
                    InProgressCases = inProgressCount,
                    PendingCases = pendingCount,
                    CompletionRate = totalCount > 0 ? Math.Round((double)completedCount / totalCount * 100, 2) : 0
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例统计失败");
                return ServiceResult<object>.Failure("获取医疗案例统计失败", ex);
            }
        }

        /// <summary>
        /// 搜索医疗案例 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());

                var cases = await _context.MedicalCases
                    .Where(m => m.IsActive && (
                        m.Id.ToString().Contains(keyword) ||
                        m.PatientId.ToString().Contains(keyword) ||
                        m.UserId.ToString().Contains(keyword) ||
                        (m.Remark != null && m.Remark.Contains(keyword))
                    ))
                    .OrderByDescending(m => m.CreateTime)
                    .Take(20)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(cases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure("搜索医疗案例失败", ex);
            }
        }

        /// <summary>
        /// 获取医疗案例历史记录 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        {
            try
            {
                // 简化实现，返回基本历史信息
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                    return ServiceResult<List<object>>.Failure("医疗案例不存在");

                var history = new List<object>
                {
                    new
                    {
                        Action = "创建",
                        Time = medicalCase.CreateTime,
                        Status = medicalCase.Status.ToString(),
                        Remark = "医疗案例创建"
                    }
                };

                if (medicalCase.UpdateTime.HasValue)
                {
                    history.Add(new
                    {
                        Action = "更新",
                        Time = medicalCase.UpdateTime.Value,
                        Status = medicalCase.Status.ToString(),
                        Remark = medicalCase.Remark ?? "医疗案例更新"
                    });
                }

                if (medicalCase.CompleteTime.HasValue)
                {
                    history.Add(new
                    {
                        Action = "完成",
                        Time = medicalCase.CompleteTime.Value,
                        Status = "Completed",
                        Remark = "医疗案例完成"
                    });
                }

                return ServiceResult<List<object>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例历史记录失败: {Id}", id);
                return ServiceResult<List<object>>.Failure("获取医疗案例历史记录失败", ex);
            }
        }
    }
}