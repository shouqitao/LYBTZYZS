using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者核心CRUD服务 - UltraThink架构
    /// 职责：基础增删改查操作，数据验证，实体状态管理
    /// </summary>
    public class PatientServiceCore
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientServiceCore> _logger;

        public PatientServiceCore(
            AppDbContext context,
            IMapper mapper,
            ILogger<PatientServiceCore> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                var dto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败: {Id}", id);
                return ServiceResult<PatientDto>.Failure($"获取患者详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 创建患者记录
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 数据验证
                var validationResult = ValidateCreateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);

                // 检查重复 - 手机号码唯一性
                var existingPatient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.PhoneNumber == dto.PhoneNumber);
                
                if (existingPatient != null)
                    return ServiceResult<PatientDto>.Failure("手机号码已存在");

                // 创建新患者
                var patient = new Patient
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Gender = dto.Gender,
                    BirthDate = dto.BirthDate,
                    PhoneNumber = dto.PhoneNumber,
                    IdNumber = dto.IdNumber,
                    Address = dto.Address,
                    EmergencyContactName = dto.EmergencyContact, // 映射到实体字段
                    EmergencyContactPhone = dto.EmergencyPhone, // 映射到实体字段
                    // Remark字段在PatientCreateDto中不存在，暂时忽略
                    Status = CommonStatus.Enabled,
                    PinYinCode = CommonHelper.GetPinyinCode(dto.Name),
                    CreatedAt = DateTime.Now // 实体使用CreatedAt，不是CreateTime
                };

                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("创建患者记录成功: {Name} ({Id})", 
                    patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "创建患者记录失败: {Name}", dto.Name);
                return ServiceResult<PatientDto>.Failure($"创建患者记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新患者记录
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");

                // 数据验证
                var validationResult = ValidateUpdateDto(dto);
                if (!validationResult.IsSuccess)
                    return ServiceResult<PatientDto>.Failure(validationResult.ErrorMessage);

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                // 检查手机号重复（排除自己）
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    var phoneExists = await _context.Patients
                        .AnyAsync(p => p.PhoneNumber == dto.PhoneNumber && p.Id != id);
                    
                    if (phoneExists)
                        return ServiceResult<PatientDto>.Failure("手机号码已存在");
                }

                // 更新字段
                _mapper.Map(dto, patient);
                patient.PinYinCode = CommonHelper.GetPinyinCode(patient.Name);
                patient.UpdateTime = DateTime.Now;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新患者记录成功: {Name} ({Id})", 
                    patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者记录失败: {Id}", id);
                return ServiceResult<PatientDto>.Failure($"更新患者记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 软删除患者记录
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("患者ID不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                // 软删除 - 标记为已删除状态
                patient.Status = CommonStatus.Disabled;
                patient.UpdateTime = DateTime.Now;
                    
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("软删除患者记录成功: {Name} ({Id})", 
                    patient.Name, patient.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者记录失败: {Id}", id);
                return ServiceResult<bool>.Failure($"删除患者记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新患者状态
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateStatusAsync(Guid id, bool isEnabled)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("患者ID不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                var oldStatus = patient.Status;
                patient.Status = isEnabled ? CommonStatus.Enabled : CommonStatus.Disabled;
                patient.UpdateTime = DateTime.Now;
                
                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新患者状态成功: {Name} ({Id}) {OldStatus} -> {NewStatus}", 
                    patient.Name, patient.Id, oldStatus, patient.Status);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者状态失败: {Id}", id);
                return ServiceResult<bool>.Failure($"更新患者状态失败: {ex.Message}");
            }
        }

        #region 私有方法

        /// <summary>
        /// 验证创建DTO
        /// </summary>
        private ServiceResult<bool> ValidateCreateDto(PatientCreateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("患者信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<bool>.Failure("姓名不能为空");

            if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                return ServiceResult<bool>.Failure("手机号码不能为空");

            if (dto.BirthDate == default)
                return ServiceResult<bool>.Failure("出生日期不能为空");

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 验证更新DTO
        /// </summary>
        private ServiceResult<bool> ValidateUpdateDto(PatientUpdateDto dto)
        {
            if (dto == null)
                return ServiceResult<bool>.Failure("患者信息不能为空");

            if (string.IsNullOrWhiteSpace(dto.Name))
                return ServiceResult<bool>.Failure("姓名不能为空");

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}