using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - UltraThink架构重构后的统一实现
    /// 合并原QueryService和BusinessService的所有功能
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            IMedicalCaseRepository repository,
            AppDbContext context,
            IMapper mapper,
            ILogger<MedicalCaseService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Query Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<MedicalCaseDetailDto>.Failure($"医疗案例不存在: {id}");
                }

                var dto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);
                return ServiceResult<MedicalCaseDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure($"获取医疗案例详情失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = _context.MedicalCases.AsNoTracking();

                // 应用搜索条件
                if (!string.IsNullOrEmpty(query.Keyword))
                {
                    queryable = queryable.Where(x =>
                        x.PatientId.ToString().Contains(query.Keyword));
                }

                // 获取总数
                var total = await queryable.CountAsync();

                // 分页查询
                var items = await queryable
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(items);

                var result = new PagedResult<MedicalCaseDto>(
                    dtos,
                    total,
                    query.PageIndex,
                    query.PageSize
                );

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var medicalCases = await _repository.GetByPatientIdAsync(patientId);
                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                var medicalCase = await _context.MedicalCases
                    .AsNoTracking()
                    .Where(x => x.PatientId == patientId && x.Status == MedicalCaseStatus.Active)
                    .OrderByDescending(x => x.CreatedAt)
                    .FirstOrDefaultAsync();

                if (medicalCase == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("没有活跃的医疗案例");
                }

                var dto = _mapper.Map<MedicalCaseDto>(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                var queryable = _context.MedicalCases.AsNoTracking();

                if (!string.IsNullOrEmpty(keyword))
                {
                    queryable = queryable.Where(x =>
                        x.PatientId.ToString().Contains(keyword));
                }

                var medicalCases = await queryable
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"搜索失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<object>>> GetHistory(Guid id)
        {
            try
            {
                // 简化实现：返回医疗案例的基本历史信息
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<List<object>>.Failure("医疗案例不存在");
                }

                var history = new List<object>
                {
                    new { Time = medicalCase.CreatedAt, Action = "创建", Status = "新建" },
                    new { Time = medicalCase.UpdatedAt, Action = "最后更新", Status = medicalCase.Status.ToString() }
                };

                return ServiceResult<List<object>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例历史失败: {Id}", id);
                return ServiceResult<List<object>>.Failure($"获取历史失败: {ex.Message}");
            }
        }

        #endregion

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                var medicalCase = _mapper.Map<LYBT.Entities.MedicalCase.MedicalCase>(dto);
                medicalCase.Id = Guid.NewGuid();
                medicalCase.Status = MedicalCaseStatus.Active;
                medicalCase.CreatedAt = DateTime.Now;
                medicalCase.UpdatedAt = DateTime.Now;

                await _repository.AddAsync(medicalCase);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<MedicalCaseDto>(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(resultDto, "医疗案例创建成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure($"创建失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure($"医疗案例不存在: {id}");
                }

                _mapper.Map(dto, medicalCase);
                medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<MedicalCaseDto>(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(resultDto, "医疗案例更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure($"更新失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                await _repository.DeleteAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                medicalCase.Status = MedicalCaseStatus.Completed;
medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例已完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> Suspend(Guid id, string reason)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                medicalCase.Status = MedicalCaseStatus.Suspended;
medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例已暂停");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> Resume(Guid id)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                if (medicalCase.Status != MedicalCaseStatus.Suspended)
                {
                    return ServiceResult<bool>.Failure("只能恢复已暂停的医疗案例");
                }

                medicalCase.Status = MedicalCaseStatus.Active;
medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例已恢复");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                medicalCase.Status = MedicalCaseStatus.Cancelled;
medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例已取消");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> UpdateStatus(Guid id, int status)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                medicalCase.Status = (MedicalCaseStatus)status;
                medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "状态更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> Archive(Guid id, string archiveReason)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure($"医疗案例不存在: {id}");
                }

                medicalCase.Status = MedicalCaseStatus.Archived;
medicalCase.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(medicalCase);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "医疗案例已归档");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"操作失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<object>> GetStatistics(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var queryable = _context.MedicalCases.AsNoTracking();

                if (startDate.HasValue)
                {
                    queryable = queryable.Where(x => x.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    queryable = queryable.Where(x => x.CreatedAt <= endDate.Value);
                }

                var statistics = new
                {
                    Total = await queryable.CountAsync(),
                    InProgress = await queryable.CountAsync(x => x.Status == MedicalCaseStatus.Active),
                    Completed = await queryable.CountAsync(x => x.Status == MedicalCaseStatus.Completed),
                    Cancelled = await queryable.CountAsync(x => x.Status == MedicalCaseStatus.Cancelled),
                    Suspended = await queryable.CountAsync(x => x.Status == MedicalCaseStatus.Suspended),
                    Archived = await queryable.CountAsync(x => x.Status == MedicalCaseStatus.Archived)
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例统计信息失败");
                return ServiceResult<object>.Failure($"获取统计失败: {ex.Message}");
            }
        }

        #endregion
    }
}
