using AutoMapper;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{

    /// <summary>
    /// 医疗案例业务逻辑服务 - UltraThink架构
    /// 职责：生命周期管理，业务规则，状态转换，批量操作
    /// </summary>
    public class MedicalCaseBusinessService : IMedicalCaseBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseBusinessService> _logger;

        public MedicalCaseBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<MedicalCaseBusinessService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                // 业务规则：检查患者是否有活跃案例（使用简化状态）
                var hasActiveCase = await _context.MedicalCases
                    .Where(mc => mc.PatientId == dto.PatientId)
                    .AnyAsync(mc => mc.Status.IsActive());

                if (hasActiveCase)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("患者已有活跃的医疗案例，请先完成或暂停当前案例");
                }

                // 创建新案例
                var medicalCase = new Entities.MedicalCase.MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    PatientName = "待获取患者姓名", // TODO: 从Patient服务获取
                    DoctorId = dto.DoctorId,
                    DoctorName = "待获取医生姓名", // TODO: 从User服务获取
                    ConsultationDate = DateTime.Now,
                    Status = MedicalCaseStatus.Active, // Record-Only: 新建医案直接设为活跃状态
                    Remark = dto.Remark
                };

                _context.MedicalCases.Add(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "创建医疗案例成功: {PatientName} - {DoctorName} ({Id})",
                    medicalCase.PatientName, medicalCase.DoctorName, medicalCase.Id);

                var resultDto = _mapper.Map<MedicalCaseDto>(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例失败: PatientId {PatientId}", dto.PatientId);
                return ServiceResult<MedicalCaseDto>.Failure($"创建医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建医疗案例并关联处方 - Phase B2 事务优化
        /// 在单个短事务中创建医案和可选的关联处方
        /// </summary>
        public async Task<ServiceResult<MedicalCaseWithPrescriptionResultDto>> CreateWithPrescriptionAsync(
            MedicalCaseWithPrescriptionCreateDto dto, Guid operatorId, string operatorName)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 数据验证
                    if (dto?.MedicalCase == null)
                    {
                        return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure("医疗案例信息不能为空");
                    }

                    if (dto.CreatePrescriptionImmediately && dto.Prescription == null)
                    {
                        return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure("要求立即创建处方但未提供处方信息");
                    }

                    // 验证医案创建信息
                    var medicalCaseValidation = ValidateCreateDto(dto.MedicalCase);
                    if (!medicalCaseValidation.IsSuccess)
                    {
                        return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure(
                            $"医案数据验证失败: {medicalCaseValidation.ErrorMessage}");
                    }

                    // 业务规则：检查患者是否有活跃案例
                    var hasActiveCase = await _context.MedicalCases
                        .Where(mc => mc.PatientId == dto.MedicalCase.PatientId)
                        .AnyAsync(mc => mc.Status.IsActive());

                    if (hasActiveCase)
                    {
                        return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure(
                            "患者已有活跃的医疗案例，请先完成或暂停当前案例");
                    }

                    // Step 1: 创建医疗案例
                    var medicalCase = new Entities.MedicalCase.MedicalCase
                    {
                        Id = Guid.NewGuid(),
                        PatientId = dto.MedicalCase.PatientId,
                        PatientName = "待获取患者姓名", // TODO: 从Patient服务获取
                        DoctorId = dto.MedicalCase.DoctorId,
                        DoctorName = "待获取医生姓名", // TODO: 从User服务获取
                        ConsultationDate = DateTime.Now,
                        Status = MedicalCaseStatus.Active,
                        Remark = dto.MedicalCase.Remark
                    };

                    _context.MedicalCases.Add(medicalCase);

                    // Step 2: 如果需要，创建关联处方
                    Prescription? prescription = null;
                    if (dto.CreatePrescriptionImmediately && dto.Prescription != null)
                    {
                        prescription = new Prescription
                        {
                            Id = Guid.NewGuid(),
                            MedicalCaseId = medicalCase.Id, // 关联到新创建的医案
                            PatientId = dto.MedicalCase.PatientId,
                            UserId = dto.MedicalCase.DoctorId,
                            Indication = dto.Prescription.Diagnosis,
                            DosageCount = dto.Prescription.DosageCount,
                            Advice = dto.Prescription.Advice,
                            Status = PrescriptionStatus.Draft,
                            Remark = dto.Prescription.Remark,
                            FormulaSource = dto.Prescription.FormulaSource ?? "医案创建时开具",
                            Discount = 1.0m
                        };

                        _context.Prescriptions.Add(prescription);

                        // 更新医案的处方关联
                        medicalCase.PrescriptionId = prescription.Id;

                        // 如果提供了处方项目，创建处方项目
                        if (dto.Prescription.Items?.Any() == true)
                        {
                            foreach (var itemDto in dto.Prescription.Items)
                            {
                                var item = new PrescriptionItem
                                {
                                    Id = Guid.NewGuid(),
                                    PrescriptionId = prescription.Id,
                                    HerbId = itemDto.HerbId,
                                    HerbName = itemDto.HerbName,
                                    Quantity = itemDto.Quantity,
                                    UnitPrice = itemDto.UnitPrice,
                                    Unit = itemDto.Unit,
                                    Usage = itemDto.Usage,
                                    Remark = itemDto.Note ?? itemDto.Remark
                                };
                                _context.PrescriptionItems.Add(item);
                            }
                        }
                    }

                    // 一次性保存所有更改
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "医案+处方联建成功 - 操作者: {OperatorName} ({OperatorId}), 医案: {MedicalCaseId}, 处方: {PrescriptionId}",
                        operatorName, operatorId, medicalCase.Id, prescription?.Id);

                    // 准备返回结果
                    var resultDto = new MedicalCaseWithPrescriptionResultDto
                    {
                        MedicalCase = _mapper.Map<MedicalCaseDto>(medicalCase),
                        Prescription = prescription != null ? _mapper.Map<PrescriptionDto>(prescription) : null,
                        IsSuccess = true,
                        Message = prescription != null ? "医案和处方创建成功" : "医案创建成功"
                    };

                    return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Success(resultDto);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "医案+处方联建并发冲突 - 操作者: {OperatorName}", operatorName);
                    return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure("数据已被其他用户修改，请刷新后重试");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "医案+处方联建失败 - 操作者: {OperatorName}", operatorName);
                    return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure($"医案+处方联建失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 更新医疗案例信息
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例ID不能为空");
                }

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage ?? "数据验证失败");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");
                }

                // 更新字段 - MedicalCaseUpdateDto不包含PatientName/DoctorName
                // 只更新可以直接更新的字段
                medicalCase.Remark = dto.Remark;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "更新医疗案例成功: {PatientName} - {DoctorName} ({Id})",
                    medicalCase.PatientName, medicalCase.DoctorName, medicalCase.Id);

                var resultDto = _mapper.Map<MedicalCaseDto>(medicalCase);
                return ServiceResult<MedicalCaseDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败: {Id}", id);
                return ServiceResult<MedicalCaseDto>.Failure($"更新医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除医疗案例（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // 业务规则：检查是否可以删除
                if (medicalCase.Status == MedicalCaseStatus.InConsultation)
                {
                    return ServiceResult<bool>.Failure("进行中的医疗案例不能删除，请先完成或暂停");
                }

                // 软删除 - 设置状态为已关闭（Record-Only简化）
                medicalCase.Status = MedicalCaseStatus.Closed;
                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除医疗案例成功: {PatientName} ({Id})", medicalCase.PatientName, medicalCase.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 完成医疗案例 - Record-Only简化：直接设为关闭状态
        /// </summary>
        [Obsolete("Complex state transition removed in Record-Only mode. Use simple status update instead.", false)]
        public async Task<ServiceResult<bool>> CompleteAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // 业务规则：只有活跃的案例可以关闭（Record-Only简化逻辑）
                if (!medicalCase.Status.IsActive())
                {
                    return ServiceResult<bool>.Failure("只有活跃的案例才能完成");
                }

                // Record-Only: 直接设为关闭状态
                medicalCase.Status = MedicalCaseStatus.Closed;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "完成医疗案例: {PatientName} ({Id})",
                    medicalCase.PatientName, medicalCase.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"完成医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        [Obsolete("Complex state transition removed in Record-Only mode. Use simple status update instead.", false)]
        public async Task<ServiceResult<bool>> SuspendAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // Record-Only: 简化业务规则，活跃状态保持不变（暂停在UI层处理）
                if (!medicalCase.Status.IsActive())
                {
                    return ServiceResult<bool>.Failure("只有活跃的案例可以操作");
                }

                // Record-Only: 暂停逻辑简化，保持活跃状态
                // 实际暂停状态由前端或其他业务字段管理
                medicalCase.Status = MedicalCaseStatus.Active;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "暂停医疗案例: {PatientName} ({Id})",
                    medicalCase.PatientName, medicalCase.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"暂停医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        [Obsolete("Complex state transition removed in Record-Only mode. Use simple status update instead.", false)]
        public async Task<ServiceResult<bool>> ResumeAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // Record-Only: 简化恢复逻辑，只需要是已关闭的案例可以重新激活
                if (medicalCase.Status.IsActive())
                {
                    return ServiceResult<bool>.Failure("案例已经是活跃状态");
                }

                // 检查患者是否已有其他活跃案例（使用简化状态）
                var hasActiveCase = await _context.MedicalCases
                    .Where(mc => mc.PatientId == medicalCase.PatientId && mc.Id != caseId)
                    .AnyAsync(mc => mc.Status.IsActive());

                if (hasActiveCase)
                {
                    return ServiceResult<bool>.Failure("患者已有其他活跃案例，无法恢复当前案例");
                }

                // Record-Only: 恢复为活跃状态
                medicalCase.Status = MedicalCaseStatus.Active;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("恢复医疗案例: {PatientName} ({Id})", medicalCase.PatientName, medicalCase.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"恢复医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        [Obsolete("Complex state transition removed in Record-Only mode. Use simple status update instead.", false)]
        public async Task<ServiceResult<bool>> ArchiveAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // Record-Only: 归档逻辑简化，只需要是关闭状态
                if (medicalCase.Status.IsActive())
                {
                    return ServiceResult<bool>.Failure("只有已关闭的案例才能归档");
                }

                // Record-Only: 归档就是关闭状态，无需额外处理
                medicalCase.Status = MedicalCaseStatus.Closed;
                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "归档医疗案例: {PatientName} ({Id})",
                    medicalCase.PatientName, medicalCase.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"归档医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        [Obsolete("Complex status strings removed in Record-Only mode. Use simplified Active/Closed states.", false)]
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid caseId, string status)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(status))
                {
                    return ServiceResult<bool>.Failure("状态值不能为空");
                }

                // Record-Only: 简化状态映射到Active/Closed二元状态
                MedicalCaseStatus medicalCaseStatus;
                switch (status.ToLower())
                {
                    // 活跃状态映射
                    case "active":
                    case "registered":
                    case "inconsultation":
                    case "suspended":
                        medicalCaseStatus = MedicalCaseStatus.Active;
                        break;

                    // 关闭状态映射
                    case "closed":
                    case "completed":
                    case "cancelled":
                    case "archived":
                        medicalCaseStatus = MedicalCaseStatus.Closed;
                        break;

                    default:
                        return ServiceResult<bool>.Failure($"无效的状态值: {status}，仅支持 active/closed 及其兼容映射");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                var oldStatus = medicalCase.Status;
                medicalCase.Status = medicalCaseStatus;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "更新医疗案例状态: {PatientName} ({Id}) {OldStatus} -> {NewStatus}",
                    medicalCase.PatientName, medicalCase.Id, oldStatus, medicalCaseStatus);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"更新医疗案例状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新医疗案例状态
        /// </summary>
        [Obsolete("Complex batch status update removed in Record-Only mode. Use simple individual updates instead.", false)]
        public async Task<ServiceResult<bool>> BatchUpdateStatusAsync(List<Guid> caseIds, string status)
        {
            try
            {
                if (caseIds == null || caseIds.Count == 0)
                {
                    return ServiceResult<bool>.Success(true);
                }

                if (string.IsNullOrWhiteSpace(status))
                {
                    return ServiceResult<bool>.Failure("状态值不能为空");
                }

                // Record-Only: 批量状态映射到Active/Closed二元状态
                MedicalCaseStatus medicalCaseStatus;
                switch (status.ToLower())
                {
                    // 活跃状态映射
                    case "active":
                    case "registered":
                    case "inconsultation":
                    case "suspended":
                        medicalCaseStatus = MedicalCaseStatus.Active;
                        break;

                    // 关闭状态映射
                    case "closed":
                    case "completed":
                    case "cancelled":
                    case "archived":
                        medicalCaseStatus = MedicalCaseStatus.Closed;
                        break;

                    default:
                        return ServiceResult<bool>.Failure($"无效的状态值: {status}，仅支持 active/closed 及其兼容映射");
                }

                var updatedCount = await _context.MedicalCases
                    .Where(mc => caseIds.Contains(mc.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(mc => mc.Status, medicalCaseStatus));

                _logger.LogInformation(
                    "批量更新医疗案例状态完成: 更新了 {Count} 条记录，状态: {Status}",
                    updatedCount, medicalCaseStatus);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新医疗案例状态失败");
                return ServiceResult<bool>.Failure($"批量更新失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 取消看诊
        /// </summary>
        [Obsolete("Complex consultation cancellation removed in Record-Only mode. Use simple status update instead.", false)]
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid caseId)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<bool>.Failure("医疗案例不存在");
                }

                // Record-Only: 简化取消逻辑，只有活跃案例可以取消
                if (!medicalCase.Status.IsActive())
                {
                    return ServiceResult<bool>.Failure("只有活跃的案例才能取消");
                }

                // Record-Only: 取消就是关闭状态
                medicalCase.Status = MedicalCaseStatus.Closed;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "取消看诊: {PatientName} ({Id})",
                    medicalCase.PatientName, medicalCase.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊失败: {Id}", caseId);
                return ServiceResult<bool>.Failure($"取消看诊失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 打印病历记录
        /// </summary>
        public async Task<ServiceResult<object>> PrintMedicalRecordAsync(Guid caseId, object printOptions)
        {
            try
            {
                if (caseId == Guid.Empty)
                {
                    return ServiceResult<object>.Failure("医疗案例ID不能为空");
                }

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == caseId);

                if (medicalCase == null)
                {
                    return ServiceResult<object>.Failure("医疗案例不存在");
                }

                // 构建打印数据
                var printData = new
                {
                    CaseId = medicalCase.Id,
                    PatientName = medicalCase.PatientName,
                    DoctorName = medicalCase.DoctorName,
                    ConsultationDate = medicalCase.ConsultationDate,
                    Status = medicalCase.Status.ToString(),
                    Remark = medicalCase.Remark,
                    PrintTime = DateTime.Now,
                    PrintOptions = printOptions
                };

                _logger.LogInformation(
                    "打印病历记录: {PatientName} ({Id})",
                    medicalCase.PatientName, medicalCase.Id);

                return ServiceResult<object>.Success(printData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打印病历记录失败: {Id}", caseId);
                return ServiceResult<object>.Failure($"打印病历记录失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(MedicalCaseCreateDto dto)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("医疗案例信息不能为空");
            }

            if (dto.PatientId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("患者ID不能为空");
            }

            // PatientName字段不在CreateDto中，由服务从PatientId获取
            // 跳过PatientName验证
            if (dto.DoctorId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("医生ID不能为空");
            }

            // DoctorName字段不在CreateDto中，由服务从DoctorId获取
            // 跳过DoctorName验证
            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证更新DTO
        /// </summary>
        private ServiceResult<bool> ValidateUpdateDto(MedicalCaseUpdateDto dto)
        {
            if (dto == null)
            {
                return ServiceResult<bool>.Failure("医疗案例信息不能为空");
            }

            // PatientName字段不在CreateDto中，由服务从PatientId获取
            // 跳过PatientName验证

            // DoctorName字段不在CreateDto中，由服务从DoctorId获取
            // 跳过DoctorName验证
            return ServiceResult<bool>.Success(true);
        }

        #endregion 私有方法
    }
}
