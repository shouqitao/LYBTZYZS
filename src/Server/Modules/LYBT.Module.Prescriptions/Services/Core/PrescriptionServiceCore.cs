using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services.Core
{
    /// <summary>
    /// 处方核心CRUD服务 - UltraThink架构
    /// 职责：基础增删改查操作，数据验证，状态管理
    /// </summary>
    public class PrescriptionServiceCore
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionServiceCore> _logger;

        public PrescriptionServiceCore(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionServiceCore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败: {Id}", id);
                return ServiceResult<PrescriptionDto>.Failure($"获取处方详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage);

                // 创建新处方
                var prescription = new Prescription
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    UserId = dto.DoctorId, // DTO中的DoctorId对应实体中的UserId
                    MedicalCaseId = dto.ConsultationId ?? Guid.Empty, // DTO中的ConsultationId对应实体中的MedicalCaseId
                    Indication = dto.Diagnosis, // DTO中的Diagnosis对应实体中的Indication
                    DosageCount = dto.DosageCount,
                    Advice = dto.Advice,
                    Status = PrescriptionStatus.Draft,
                    Remark = dto.Remark,
                    FormulaSource = dto.FormulaSource
                };

                _context.Prescriptions.Add(prescription);
                
                // 处理处方项目
                if (dto.Items != null && dto.Items.Any())
                {
                    foreach (var item in dto.Items)
                    {
                        var prescriptionItem = new PrescriptionItemModel
                        {
                            Id = Guid.NewGuid(),
                            PrescriptionId = prescription.Id,
                            HerbId = item.HerbId,
                            HerbName = item.HerbName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Unit = item.Unit,
                            Usage = item.Usage,
                            Remark = item.Note
                        };
                        _context.PrescriptionItems.Add(prescriptionItem);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("创建处方成功: {Indication} ({Id})", 
                    prescription.Indication, prescription.Id);

                var resultDto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "创建处方失败: {Diagnosis}", dto.Diagnosis);
                return ServiceResult<PrescriptionDto>.Failure($"创建处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新处方信息
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage);

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                // 更新字段 - 使用AutoMapper映射避免字段遗漏
                _mapper.Map(dto, prescription);

                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新处方成功: {Indication} ({Id})", 
                    prescription.Indication, prescription.Id);

                var resultDto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败: {Id}", id);
                return ServiceResult<PrescriptionDto>.Failure($"更新处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 软删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("处方ID不能为空");

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                    return ServiceResult<bool>.Failure("处方不存在");

                // 软删除 - 添加删除标记到备注
                prescription.Remark = string.IsNullOrEmpty(prescription.Remark) 
                    ? "处方已删除" 
                    : $"{prescription.Remark}\n处方已删除";
                    
                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除处方成功: {Indication} ({Id})", prescription.Indication, prescription.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新处方状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, PrescriptionStatus status)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("处方ID不能为空");

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                    return ServiceResult<bool>.Failure("处方不存在");

                var oldStatus = prescription.Status;
                prescription.Status = status;
                
                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新处方状态成功: {Indication} ({Id}) {OldStatus} -> {NewStatus}", 
                    prescription.Indication, prescription.Id, oldStatus, status);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"更新处方状态失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(PrescriptionCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("处方信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                return ServiceResult<bool>.Failure("诊断不能为空");

            if (dto.PatientId == Guid.Empty)
                return ServiceResult<bool>.Failure("患者ID不能为空");

            if (dto.DoctorId == Guid.Empty)
                return ServiceResult<bool>.Failure("医生ID不能为空");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证更新DTO
        /// </summary>
        private ServiceResult<bool> ValidateUpdateDto(PrescriptionEditDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("处方信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                return ServiceResult<bool>.Failure("诊断不能为空");

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}