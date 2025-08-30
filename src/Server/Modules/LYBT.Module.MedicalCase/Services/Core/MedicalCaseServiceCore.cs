using System;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services.Core
{
    /// <summary>
    /// 医疗案例核心CRUD服务 - UltraThink架构
    /// 职责：基础增删改查操作，状态管理，数据验证
    /// </summary>
    public class MedicalCaseServiceCore
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseServiceCore> _logger;

        public MedicalCaseServiceCore(
            AppDbContext context,
            IMapper mapper,
            ILogger<MedicalCaseServiceCore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .Include(mc => mc.Consultation)
                    .Include(mc => mc.Prescription)
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例不存在");

                var dto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);
                return ServiceResult<MedicalCaseDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure($"获取医疗案例详情失败: {ex.Message}");
            }
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

                // 检查患者是否有活跃案例
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
        /// 软删除医疗案例
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

                // 检查是否可以删除
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
        /// 更新医疗案例状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, MedicalCaseStatus status)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<bool>.Failure("医疗案例不存在");

                var oldStatus = medicalCase.Status;
                medicalCase.Status = status;
                
                _context.MedicalCases.Update(medicalCase);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新医疗案例状态成功: {PatientName} ({Id}) {OldStatus} -> {NewStatus}", 
                    medicalCase.PatientName, medicalCase.Id, oldStatus, status);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"更新医疗案例状态失败: {ex.Message}");
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