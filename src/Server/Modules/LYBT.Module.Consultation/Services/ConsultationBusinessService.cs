using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{

    /// <summary>
    /// 诊疗业务服务 - UltraThink架构
    /// 职责：业务逻辑处理，工作流管理，状态变更，中医四诊处理
    /// </summary>
    public class ConsultationBusinessService : IConsultationBusinessService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationBusinessService> _logger;

        public ConsultationBusinessService(
            IConsultationRepository repository,
            IMapper mapper,
            ILogger<ConsultationBusinessService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 保存四诊数据
        /// </summary>
        

        /// <summary>
        /// 开始诊疗
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("诊疗启动数据不能为空");
                }

                if (dto.PatientId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("患者ID不能为空");
                }

                if (dto.MedicalCaseId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("医疗案例ID不能为空");
                }

                if (dto.UserId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("医生ID不能为空");
                }

                // 检查是否已存在进行中的诊疗
                var existingConsultation = await _repository.GetByMedicalCaseIdAsync(dto.MedicalCaseId);

                if (existingConsultation != null)
                {
                    return ServiceResult<ConsultationDto>.Failure("该医疗案例已存在进行中的诊疗记录");
                }

                // 创建新的诊疗记录
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    MedicalCaseId = dto.MedicalCaseId,
                    UserId = dto.UserId,
                    ChiefComplaint = dto.InitialComplaint, // 修正：使用InitialComplaint字段
                    Status = CommonStatus.Enabled
                };

                await _repository.AddAsync(consultation);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                _logger.LogInformation("开始诊疗成功 - 患者: {PatientId}, 医案: {MedicalCaseId}", dto.PatientId, dto.MedicalCaseId);

                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始诊疗失败");
                return ServiceResult<ConsultationDto>.Failure($"开始诊疗失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新诊疗记录
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("诊疗ID不能为空");
                }

                if (dto == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("更新数据不能为空");
                }

                var consultation = await _repository.GetByIdAsync(id);

                if (consultation == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");
                }

                if (consultation.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<ConsultationDto>.Failure("已完成的诊疗不能修改");
                }

                // 更新诊疗信息
                consultation.ChiefComplaint = dto.ChiefComplaint;
                consultation.PresentIllness = dto.PresentIllness;
                consultation.Inspection = dto.Inspection;
                consultation.AuscultationOlfaction = dto.AuscultationOlfaction;
                consultation.Inquiry = dto.Inquiry;
                consultation.Palpation = dto.Palpation;
                consultation.TCMDiagnosis = dto.TCMDiagnosis ?? string.Empty; // 修正：使用TCMDiagnosis字段
                consultation.MedicalAdvice = dto.MedicalAdvice; // 修正：使用MedicalAdvice字段

                await _repository.UpdateAsync(consultation);
                await _repository.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                _logger.LogInformation("更新诊疗记录成功 - ID: {Id}", id);

                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊疗记录失败 - ID: {Id}", id);
                return ServiceResult<ConsultationDto>.Failure($"更新诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除诊疗记录
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("诊疗ID不能为空");
                }

                var consultation = await _repository.GetByIdAsync(id);

                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure("诊疗记录不存在");
                }

                // 软删除：设置状态为禁用
                consultation.Status = CommonStatus.Disabled;
                await _repository.UpdateAsync(consultation);
                await _repository.SaveChangesAsync();

                _logger.LogInformation("删除诊疗记录成功 - ID: {Id}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除诊疗记录失败 - ID: {Id}", id);
                return ServiceResult<bool>.Failure($"删除诊疗记录失败: {ex.Message}");
            }
        }
    }
}
