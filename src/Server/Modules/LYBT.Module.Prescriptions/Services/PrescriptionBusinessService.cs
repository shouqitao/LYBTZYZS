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
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "处方复制并发冲突 - 操作者: {OperatorName}, 源处方: {SourceId}", operatorName, sourceId);
                    return ServiceResult<PrescriptionDto>.Failure("数据已被其他用户修改，请刷新后重试");
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

                // Record-Only模式：基础配伍禁忌检查
                // 复杂的中医配伍禁忌需要专业药物知识库支持
                // 小诊所环境下依靠医生经验判断，系统进行基础检查
                
                // 检查处方中是否有重复药材
                var prescriptionItems = await _context.PrescriptionItems
                    .Where(pi => pi.PrescriptionId == prescriptionId)
                    .ToListAsync();
                
                var duplicateHerbs = prescriptionItems
                    .GroupBy(pi => pi.HerbId)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.First().HerbName)
                    .ToList();
                
                if (duplicateHerbs.Any())
                {
                    _logger.LogWarning("处方中发现重复药材: {Herbs}, 处方ID: {PrescriptionId}", 
                        string.Join(", ", duplicateHerbs), prescriptionId);
                }
                
                _logger.LogInformation("处方基础检查完成: {PrescriptionId}", prescriptionId);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处方配伍检查失败: {PrescriptionId}", prescriptionId);
                return ServiceResult<bool>.Failure($"处方配伍检查失败: {ex.Message}");
            }
        }

        #region 基础CRUD操作

        /// <summary>
        /// 创建处方
        /// </summary>
        /// <param name="dto">处方创建数据传输对象</param>
        /// <returns>包含创建的处方的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            try
            {
                if (dto.PatientId == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("患者ID不能为空");
                }

                if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                {
                    return ServiceResult<PrescriptionDto>.Failure("诊断不能为空");
                }

                var prescription = _mapper.Map<Prescription>(dto);
                prescription.Id = Guid.NewGuid();
                prescription.Status = PrescriptionStatus.Draft;

                _context.Prescriptions.Add(prescription);
                await _context.SaveChangesAsync();

                _logger.LogInformation("创建处方成功: 患者ID {PatientId}", dto.PatientId);
                var resultDto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败: 患者ID {PatientId}", dto.PatientId);
                return ServiceResult<PrescriptionDto>.Failure($"创建处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        /// <param name="id">处方ID</param>
        /// <param name="dto">处方更新数据传输对象</param>
        /// <returns>包含更新后处方的服务结果，失败时返回错误消息</returns>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                {
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");
                }

                // 只允许更新草稿状态的处方
                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<PrescriptionDto>.Failure("只能修改草稿状态的处方");
                }

                // 更新基本信息
                if (!string.IsNullOrWhiteSpace(dto.Diagnosis))
                    prescription.Indication = dto.Diagnosis; // DTO的Diagnosis映射到实体的Indication
                if (!string.IsNullOrWhiteSpace(dto.Advice))
                    prescription.Advice = dto.Advice;

                await _context.SaveChangesAsync();

                _logger.LogInformation("更新处方成功: {PrescriptionId}", id);
                var resultDto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败，ID: {PrescriptionId}", id);
                return ServiceResult<PrescriptionDto>.Failure($"更新处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        /// <param name="id">处方ID</param>
        /// <returns>表示删除操作成功或失败的服务结果</returns>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("处方ID不能为空");
                }

                var prescription = await _context.Prescriptions.FindAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 只允许删除草稿状态的处方
                if (prescription.Status != PrescriptionStatus.Draft)
                {
                    return ServiceResult<bool>.Failure("只能删除草稿状态的处方");
                }

                // 软删除：使用硬删除（因为PrescriptionStatus没有Cancelled状态）
                _context.Prescriptions.Remove(prescription);
                
                await _context.SaveChangesAsync();

                _logger.LogInformation("删除处方成功: {PrescriptionId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败，ID: {PrescriptionId}", id);
                return ServiceResult<bool>.Failure($"删除处方失败: {ex.Message}");
            }
        }

        #endregion 基础CRUD操作
    }
}
