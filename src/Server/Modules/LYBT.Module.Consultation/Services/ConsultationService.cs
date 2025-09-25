using AutoMapper;
using LYBT.Infrastructure;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - UltraThink架构重构后的统一实现
    /// 合并原QueryService和BusinessService的所有功能
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationRepository repository,
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Query Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var consultation = await _repository.GetByIdAsync(id);
                if (consultation == null)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure($"诊疗记录不存在: {id}");
                }

                var dto = _mapper.Map<ConsultationDetailDto>(consultation);
                return ServiceResult<ConsultationDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗详情失败: {Id}", id);
                return ServiceResult<ConsultationDetailDto>.Failure($"获取诊疗详情失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = _context.Consultations.AsNoTracking();

                // 应用搜索条件
                if (!string.IsNullOrEmpty(query.Keyword))
                {
                    queryable = queryable.Where(x =>
                        x.ChiefComplaint.Contains(query.Keyword));
                }

                // 获取总数
                var total = await queryable.CountAsync();

                // 分页查询
                var items = await queryable
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(items);

                var result = new PagedResult<ConsultationDto>(
                    dtos,
                    total,
                    query.PageIndex,
                    query.PageSize
                );

                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询诊疗记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var consultations = await _repository.GetByPatientIdAsync(patientId);
                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取诊疗记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                var consultations = consultation != null ? new List<LYBT.Entities.Consultation.Consultation> { consultation } : new List<LYBT.Entities.Consultation.Consultation>();
                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                var consultations = await _repository.GetByDoctorIdAsync(doctorId);
                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取诊疗记录失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                var queryable = _context.Consultations.AsNoTracking();

                if (!string.IsNullOrEmpty(keyword))
                {
                    queryable = queryable.Where(x =>
                        x.ChiefComplaint.Contains(keyword));
                }

                var consultations = await queryable
                    .OrderByDescending(x => x.CreatedAt)
                    .Take(100)
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索诊疗记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure($"搜索失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                var consultations = await _repository.GetByPatientIdAsync(patientId);
                var orderedConsultations = consultations.OrderByDescending(x => x.CreatedAt).ToList();
                var dtos = _mapper.Map<List<ConsultationDto>>(orderedConsultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者诊疗历史失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"查询失败: {ex.Message}");
            }
        }

        #endregion

        #region Business Operations

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            try
            {
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    ChiefComplaint = "诊疗记录",
                    Status = CommonStatus.Enabled,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                await _repository.AddAsync(consultation);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                return ServiceResult<ConsultationDto>.Success(resultDto, "诊疗开始成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始诊疗失败");
                return ServiceResult<ConsultationDto>.Failure($"开始诊疗失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            try
            {
                var consultation = await _repository.GetByIdAsync(id);
                if (consultation == null)
                {
                    return ServiceResult<ConsultationDto>.Failure($"诊疗记录不存在: {id}");
                }

                // 更新实体
                consultation.ChiefComplaint = dto.ChiefComplaint;
                consultation.UpdatedAt = DateTime.Now;

                await _repository.UpdateAsync(consultation);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                return ServiceResult<ConsultationDto>.Success(resultDto, "诊疗记录更新成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊疗记录失败: {Id}", id);
                return ServiceResult<ConsultationDto>.Failure($"更新失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var consultation = await _repository.GetByIdAsync(id);
                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure($"诊疗记录不存在: {id}");
                }

                await _repository.DeleteAsync(consultation);
                await _repository.SaveChangesAsync();

                return ServiceResult<bool>.Success(true, "诊疗记录删除成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除诊疗记录失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除失败: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var queryable = _context.Consultations.AsNoTracking();

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
                    Enabled = await queryable.CountAsync(x => x.Status == CommonStatus.Enabled),
                    Disabled = await queryable.CountAsync(x => x.Status == CommonStatus.Disabled)
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗统计信息失败");
                return ServiceResult<object>.Failure($"获取统计失败: {ex.Message}");
            }
        }

        #endregion
    }
}