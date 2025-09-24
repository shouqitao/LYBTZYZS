using AutoMapper;
using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Data;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例业务逻辑服务 - 简化版本
    /// 职责：增删查改和诊疗流程，符合用户要求
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
                    .AnyAsync(mc => mc.Status == MedicalCaseStatus.Active);

                if (hasActiveCase)
                {
                    return ServiceResult<MedicalCaseDto>.Failure("患者已有活跃的医疗案例，请先完成或关闭当前案例");
                }

                // 获取患者和医生姓名 - Record-Only模式：直接查询数据库保持简单
                var patient = await _context.Patients.AsNoTracking()
                    .Where(p => p.Id == dto.PatientId)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync();
                var doctor = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == dto.DoctorId)
                    .Select(u => u.RealName)
                    .FirstOrDefaultAsync();

                // 创建新案例
                var medicalCase = new Entities.MedicalCase.MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    PatientName = patient ?? "未知患者",
                    DoctorId = dto.DoctorId,
                    DoctorName = doctor ?? "未知医生",
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
                        .AnyAsync(mc => mc.Status == MedicalCaseStatus.Active);

                    if (hasActiveCase)
                    {
                        return ServiceResult<MedicalCaseWithPrescriptionResultDto>.Failure(
                            "患者已有活跃的医疗案例，请先完成或关闭当前案例");
                    }

                    // Step 1: 获取患者和医生姓名 - Record-Only模式：直接查询数据库保持简单
                    var patient = await _context.Patients.AsNoTracking()
                        .Where(p => p.Id == dto.MedicalCase.PatientId)
                        .Select(p => p.Name)
                        .FirstOrDefaultAsync();
                    var doctor = await _context.Users.AsNoTracking()
                        .Where(u => u.Id == dto.MedicalCase.DoctorId)
                        .Select(u => u.RealName)
                        .FirstOrDefaultAsync();

                    // Step 2: 创建医疗案例
                    var medicalCase = new Entities.MedicalCase.MedicalCase
                    {
                        Id = Guid.NewGuid(),
                        PatientId = dto.MedicalCase.PatientId,
                        PatientName = patient ?? "未知患者",
                        DoctorId = dto.MedicalCase.DoctorId,
                        DoctorName = doctor ?? "未知医生",
                        ConsultationDate = DateTime.Now,
                        Status = MedicalCaseStatus.Active,
                        Remark = dto.MedicalCase.Remark
                    };

                    _context.MedicalCases.Add(medicalCase);

                    // Step 3: 如果需要，创建关联处方
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

                        // 根据文档要求，删除PrescriptionId字段，通过Prescription.MedicalCaseId关联
                        // medicalCase.PrescriptionId = prescription.Id; // 已删除此字段

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
                                    Quantity = (int)itemDto.Quantity, // 强制转换为int
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
                if (medicalCase.Status == MedicalCaseStatus.Active)
                {
                    return ServiceResult<bool>.Failure("活跃的医疗案例不能删除，请先关闭");
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

            if (dto.DoctorId == Guid.Empty)
            {
                return ServiceResult<bool>.Failure("医生ID不能为空");
            }

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

            return ServiceResult<bool>.Success(true);
        }

        #endregion 私有方法
    }
}