using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例业务逻辑服务 - UltraThink架构
    /// 职责：生命周期管理，业务规则，状态转换，批量操作
    /// </summary>
    public class MedicalCaseBusinessService
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
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage);

                // 业务规则：检查患者是否有活跃案例
                var hasActiveCase = await _context.MedicalCases
                    .AnyAsync(mc => mc.PatientId == dto.PatientId && 
                                  mc.Status == MedicalCaseStatus.InConsultation);

                if (hasActiveCase)
                    return ServiceResult<MedicalCaseDto>.Failure("患者已有活跃的医疗案例，请先完成或暂停当前案例");

                // 创建新案例
                var medicalCase = new Entities.MedicalCase.MedicalCase
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    PatientName = "待获取患者姓名", // TODO: 从Patient服务获取
                    DoctorId = dto.DoctorId,
                    DoctorName = "待获取医生姓名", // TODO: 从User服务获取
                    ConsultationDate = DateTime.Now,
                    Status = MedicalCaseStatus.Registered,
                    Remark = dto.Remark
                };

                _context.MedicalCases.Add(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("创建医疗案例成功: {PatientName} - {DoctorName} ({Id})", 
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
        /// 更新医疗案例信息
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例ID不能为空");

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<MedicalCaseDto>.Failure(validationResult.ErrorMessage);

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

                // 更新字段 - MedicalCaseUpdateDto不包含PatientName/DoctorName
                // 只更新可以直接更新的字段
                medicalCase.Remark = dto.Remark;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新医疗案例成功: {PatientName} - {DoctorName} ({Id})", 
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
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                // 业务规则：检查是否可以删除
                if (medicalCase.Status == MedicalCaseStatus.InConsultation)
                    return ServiceResult<bool>.Failure("进行中的医疗案例不能删除，请先完成或暂停");

                // 软删除 - 设置状态为取消
                medicalCase.Status = MedicalCaseStatus.Cancelled;
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
        /// 完成医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteAsync(Guid id, string completionReason)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                // 业务规则：只有进行中的案例可以完成
                if (medicalCase.Status != MedicalCaseStatus.InConsultation)
                    return ServiceResult<bool>.Failure("只有进行中的案例才能完成");

                // 更新状态
                medicalCase.Status = MedicalCaseStatus.Completed;
                if (!string.IsNullOrWhiteSpace(completionReason))
                {
                    medicalCase.Remark = string.IsNullOrWhiteSpace(medicalCase.Remark) 
                        ? $"完成原因: {completionReason}"
                        : $"{medicalCase.Remark}\n完成原因: {completionReason}";
                }

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("完成医疗案例: {PatientName} ({Id}), 原因: {Reason}", 
                    medicalCase.PatientName, medicalCase.Id, completionReason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"完成医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 暂停医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> SuspendAsync(Guid id, string reason)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                // 业务规则：只有进行中的案例可以暂停
                if (medicalCase.Status != MedicalCaseStatus.InConsultation)
                    return ServiceResult<bool>.Failure("只有进行中的案例才能暂停");

                // 更新状态（使用已注册状态表示暂停）
                medicalCase.Status = MedicalCaseStatus.Registered;
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    medicalCase.Remark = string.IsNullOrWhiteSpace(medicalCase.Remark) 
                        ? $"暂停原因: {reason}"
                        : $"{medicalCase.Remark}\n暂停原因: {reason}";
                }

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("暂停医疗案例: {PatientName} ({Id}), 原因: {Reason}", 
                    medicalCase.PatientName, medicalCase.Id, reason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "暂停医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"暂停医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 恢复医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> ResumeAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                // 业务规则：只有已注册的案例可以恢复为进行中
                if (medicalCase.Status != MedicalCaseStatus.Registered)
                    return ServiceResult<bool>.Failure("只有已注册或暂停的案例才能恢复");

                // 检查患者是否已有其他活跃案例
                var hasActiveCase = await _context.MedicalCases
                    .AnyAsync(mc => mc.PatientId == medicalCase.PatientId && 
                                  mc.Id != id &&
                                  mc.Status == MedicalCaseStatus.InConsultation);

                if (hasActiveCase)
                    return ServiceResult<bool>.Failure("患者已有其他活跃案例，无法恢复当前案例");

                // 恢复为进行中状态
                medicalCase.Status = MedicalCaseStatus.InConsultation;

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("恢复医疗案例: {PatientName} ({Id})", medicalCase.PatientName, medicalCase.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"恢复医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 归档医疗案例
        /// </summary>
        public async Task<ServiceResult<bool>> ArchiveAsync(Guid id, string archiveReason)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                // 业务规则：只有已完成的案例可以归档
                if (medicalCase.Status != MedicalCaseStatus.Completed)
                    return ServiceResult<bool>.Failure("只有已完成的案例才能归档");

                // 归档处理（在当前系统中，归档状态和完成状态相同）
                if (!string.IsNullOrWhiteSpace(archiveReason))
                {
                    medicalCase.Remark = string.IsNullOrWhiteSpace(medicalCase.Remark) 
                        ? $"归档原因: {archiveReason}"
                        : $"{medicalCase.Remark}\n归档原因: {archiveReason}";
                }

                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("归档医疗案例: {PatientName} ({Id}), 原因: {Reason}", 
                    medicalCase.PatientName, medicalCase.Id, archiveReason);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "归档医疗案例失败: {Id}", id);
                return ServiceResult<bool>.Failure($"归档医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, int status)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                if (!Enum.IsDefined(typeof(MedicalCaseStatus), status))
                    return ServiceResult<bool>.Failure($"无效的状态值: {status}");

                var medicalCaseStatus = (MedicalCaseStatus)status;
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                var oldStatus = medicalCase.Status;
                medicalCase.Status = medicalCaseStatus;
                
                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新医疗案例状态: {PatientName} ({Id}) {OldStatus} -> {NewStatus}", 
                    medicalCase.PatientName, medicalCase.Id, oldStatus, medicalCaseStatus);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"更新医疗案例状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(Guid[] ids, MedicalCaseStatus status)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                    return ServiceResult<int>.Success(0);

                var updatedCount = await _context.MedicalCases
                    .Where(mc => ids.Contains(mc.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(mc => mc.Status, status));

                _logger.LogInformation("批量更新医疗案例状态完成: 更新了 {Count} 条记录，状态: {Status}", 
                    updatedCount, status);

                return ServiceResult<int>.Success(updatedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新医疗案例状态失败");
                return ServiceResult<int>.Failure($"批量更新失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(MedicalCaseCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("医疗案例信息不能为空");

            if (dto.PatientId == Guid.Empty)
                return ServiceResult<bool>.Failure("患者ID不能为空");

            // PatientName字段不在CreateDto中，由服务从PatientId获取
            // 跳过PatientName验证

            if (dto.DoctorId == Guid.Empty)
                return ServiceResult<bool>.Failure("医生ID不能为空");

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
                return ServiceResult<bool>.Failure("医疗案例信息不能为空");

            // PatientName字段不在CreateDto中，由服务从PatientId获取
            // 跳过PatientName验证

            // DoctorName字段不在CreateDto中，由服务从DoctorId获取
            // 跳过DoctorName验证

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}