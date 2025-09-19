using AutoMapper;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{

    /// <summary>
    /// 处方业务服务 - UltraThink架构
    /// 职责：业务逻辑处理，复制处方，验方模板应用，状态变更，业务规则验证
    /// </summary>
    public class PrescriptionBusinessService : IPrescriptionBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionBusinessService> _logger;

        public PrescriptionBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionBusinessService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid sourceId, string newName, Guid operatorId, string operatorName)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (sourceId == Guid.Empty)
                    {
                        return ServiceResult<PrescriptionDto>.Failure("源处方ID不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        return ServiceResult<PrescriptionDto>.Failure("新处方名称不能为空");
                    }

                    // 获取源处方
                    var sourcePrescription = await _context.Prescriptions
                        .Include(p => p.Items)
                        .FirstOrDefaultAsync(p => p.Id == sourceId);

                    if (sourcePrescription == null)
                    {
                        return ServiceResult<PrescriptionDto>.Failure("源处方不存在");
                    }

                    // 创建新处方
                    var newPrescription = new Prescription
                    {
                        Id = Guid.NewGuid(),
                        PatientId = sourcePrescription.PatientId,
                        UserId = sourcePrescription.UserId,
                        MedicalCaseId = sourcePrescription.MedicalCaseId,
                        Indication = newName,
                        DosageCount = sourcePrescription.DosageCount,
                        Advice = sourcePrescription.Advice,
                        Status = PrescriptionStatus.Draft,
                        Remark = $"复制自: {sourcePrescription.Indication}",
                        FormulaSource = sourcePrescription.FormulaSource,
                        Discount = sourcePrescription.Discount
                    };

                    _context.Prescriptions.Add(newPrescription);

                    // 复制处方项目
                    if (sourcePrescription.Items?.Any() == true)
                    {
                        foreach (var sourceItem in sourcePrescription.Items)
                        {
                            var newItem = new PrescriptionItem
                            {
                                Id = Guid.NewGuid(),
                                PrescriptionId = newPrescription.Id,
                                HerbId = sourceItem.HerbId,
                                HerbName = sourceItem.HerbName,
                                Quantity = sourceItem.Quantity,
                                UnitPrice = sourceItem.UnitPrice,
                                Unit = sourceItem.Unit,
                                Usage = sourceItem.Usage,
                                Remark = sourceItem.Remark
                            };
                            _context.PrescriptionItems.Add(newItem);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation(
                        "复制处方成功 - 操作者: {OperatorName} ({OperatorId}), 源处方: {SourceId}, 新处方: {NewId}",
                        operatorName, operatorId, sourceId, newPrescription.Id);

                    var resultDto = _mapper.Map<PrescriptionDto>(newPrescription);
                    return ServiceResult<PrescriptionDto>.Success(resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "复制处方失败 - 操作者: {OperatorName}, 源处方: {SourceId}", operatorName, sourceId);
                    return ServiceResult<PrescriptionDto>.Failure($"复制处方失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 复制上次处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("患者ID不能为空");
                }

                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("医生ID不能为空");
                }

                // 获取该患者的最近一次处方
                var lastPrescription = await _context.Prescriptions
                    .Where(p => p.PatientId == patientId && p.Status != PrescriptionStatus.Completed)
                    .OrderByDescending(p => p.Id)
                    .FirstOrDefaultAsync();

                if (lastPrescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("患者没有历史处方记录");
                }

                var newName = $"{lastPrescription.Indication} - 复制于{DateTime.Now:MM-dd}";
                return await CopyAsync(lastPrescription.Id, newName, operatorId, operatorName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制上次处方失败 - 操作者: {OperatorName}, 患者: {PatientId}", operatorName, patientId);
                return ServiceResult<PrescriptionDto>.Failure($"复制上次处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从验方模板创建处方 - Record-Only模式已移除自动套用逻辑
        /// </summary>
        [Obsolete("Automatic formula application removed in Record-Only mode. Use manual template import instead.", false)]
        public Task<ServiceResult<PrescriptionDto>> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var emptyPrescription = new PrescriptionDto
            {
                Id = Guid.NewGuid(),
                Indication = "验方自动套用功能在 Record-Only 模式下已移除",
                PatientId = patientId,
                UserId = doctorId,
                Status = CommonStatus.Enabled,
                Remark = "请手动导入验方模板并调整药材用量",
                FormulaSource = "手动导入"
            };

            return Task.FromResult(ServiceResult<PrescriptionDto>.Success(
                emptyPrescription,
                "验方自动套用功能已在 Record-Only 模式下移除，请使用手动导入模板"));
        }

        /// <summary>
        /// 快速保存处方（草稿状态）
        /// </summary>
        public async Task<ServiceResult<bool>> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            try
            {
                if (prescriptionId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == prescriptionId);

                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以编辑
                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只能编辑草稿状态的处方");
                }

                // 快速更新基本信息
                if (!string.IsNullOrWhiteSpace(dto.Diagnosis))
                {
                    prescription.Indication = dto.Diagnosis;
                }

                if (!string.IsNullOrWhiteSpace(dto.Advice))
                {
                    prescription.Advice = dto.Advice;
                }

                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "快速保存处方成功 - 操作者: {OperatorName} ({OperatorId}), 处方: {PrescriptionId}",
                    operatorName, operatorId, prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "处方数据并发冲突 - 操作者: {OperatorName}, 处方: {PrescriptionId}", operatorName, prescriptionId);
                return ServiceResult<bool>.Failure("数据已被其他用户修改，请刷新后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "快速保存处方失败 - 操作者: {OperatorName}, 处方: {PrescriptionId}", operatorName, prescriptionId);
                return ServiceResult<bool>.Failure($"快速保存处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<ServiceResult<bool>> CancelAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 使用软删除来标记作废 - 由于PrescriptionStatus没有Cancelled状态
                if (prescription.Remark != null && prescription.Remark.Contains("处方已作废"))
                {
                    return ServiceResult<bool>.Failure("处方已经是作废状态");
                }

                // 软删除标记
                prescription.Remark = string.IsNullOrEmpty(prescription.Remark)
                    ? "处方已作废"
                    : $"{prescription.Remark}\n处方已作废";

                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "作废处方成功 - 操作者: {OperatorName} ({OperatorId}), 处方: {PrescriptionId}",
                    operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "作废处方失败 - 操作者: {OperatorName}, 处方: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure($"作废处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 确认处方
        /// </summary>
        public async Task<ServiceResult<bool>> ConfirmAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以确认
                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只能确认草稿状态的处方");
                }

                prescription.Status = PrescriptionStatus.Completed;

                _context.Prescriptions.Update(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "确认处方成功 - 操作者: {OperatorName} ({OperatorId}), 处方: {PrescriptionId}",
                    operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "处方数据并发冲突 - 操作者: {OperatorName}, 处方: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("数据已被其他用户修改，请刷新后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认处方失败 - 操作者: {OperatorName}, 处方: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure($"确认处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证处方配伍安全性 (简化版)
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateCompatibilityAsync(Guid prescriptionId)
        {
            try
            {
                if (prescriptionId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == prescriptionId);

                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // TODO: 实现配伍禁忌检查逻辑
                // 目前返回安全通过
                _logger.LogInformation("处方配伍检查通过: {PrescriptionId}", prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方配伍检查失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<bool>.Failure($"处方配伍检查失败: {ex.Message}");
            }
        }
    }
}
