using System;
using System.Collections.Generic;
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
    /// 患者业务服务 - UltraThink架构
    /// 职责：业务逻辑处理，批量操作，导入导出，状态管理
    /// </summary>
    public class PatientBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientBusinessService> _logger;

        public PatientBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PatientBusinessService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 启用患者
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("患者ID不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                if (patient.Status == CommonStatus.Enabled)
                    return ServiceResult<bool>.Failure("患者已经是启用状态");

                patient.Status = CommonStatus.Enabled;
                patient.UpdateTime = DateTime.Now;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("启用患者成功: {Name} ({Id})", patient.Name, patient.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用患者失败: {Id}", id);
                return ServiceResult<bool>.Failure($"启用患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 禁用患者
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<bool>.Failure("患者ID不能为空");

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (patient == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                if (patient.Status == CommonStatus.Disabled)
                    return ServiceResult<bool>.Failure("患者已经是禁用状态");

                // 检查是否有活跃的医疗案例
                var hasActiveCases = await _context.MedicalCases
                    .AnyAsync(mc => mc.PatientId == id && mc.Status != MedicalCaseStatus.Completed);

                if (hasActiveCases)
                    return ServiceResult<bool>.Failure("患者有活跃的医疗案例，无法禁用");

                patient.Status = CommonStatus.Disabled;
                patient.UpdateTime = DateTime.Now;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("禁用患者成功: {Name} ({Id})", patient.Name, patient.Id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用患者失败: {Id}", id);
                return ServiceResult<bool>.Failure($"禁用患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量导入患者
        /// </summary>
        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (patients == null || !patients.Any())
                    return ServiceResult<object>.Failure("导入数据不能为空");

                var successCount = 0;
                var failureCount = 0;
                var errors = new List<string>();

                foreach (var patientDto in patients)
                {
                    try
                    {
                        // 检查重复手机号
                        var existingPatient = await _context.Patients
                            .AnyAsync(p => p.PhoneNumber == patientDto.PhoneNumber);

                        if (existingPatient)
                        {
                            errors.Add($"患者 {patientDto.Name} 手机号 {patientDto.PhoneNumber} 已存在");
                            failureCount++;
                            continue;
                        }

                        var patient = new Patient
                        {
                            Id = Guid.NewGuid(),
                            Name = patientDto.Name,
                            Gender = patientDto.Gender,
                            BirthDate = patientDto.BirthDate,
                            PhoneNumber = patientDto.PhoneNumber,
                            IdNumber = patientDto.IdNumber,
                            Address = patientDto.Address,
                            EmergencyContactName = patientDto.EmergencyContact, // 映射到实体字段
                            EmergencyContactPhone = patientDto.EmergencyPhone, // 映射到实体字段
                            AllergyHistory = patientDto.AllergyHistory, // 使用正确的字段映射
                            Status = CommonStatus.Enabled,
                            PinYinCode = CommonHelper.GetPinyinCode(patientDto.Name),
                            CreatedAt = DateTime.Now // 实体使用CreatedAt字段
                        };

                        _context.Patients.Add(patient);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"患者 {patientDto.Name} 导入失败: {ex.Message}");
                        failureCount++;
                        _logger.LogError(ex, "导入患者失败: {Name}", patientDto.Name);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("批量导入患者完成 - 成功: {SuccessCount}, 失败: {FailureCount}", 
                    successCount, failureCount);

                var result = new
                {
                    SuccessCount = successCount,
                    FailureCount = failureCount,
                    Errors = errors
                };

                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "批量导入患者失败");
                return ServiceResult<object>.Failure($"批量导入患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
        {
            try
            {
                var patientsQuery = _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    patientsQuery = patientsQuery.Where(p =>
                        (p.Name != null && p.Name.Contains(query.Keyword)) ||
                        (p.PhoneNumber != null && p.PhoneNumber.Contains(query.Keyword)));
                }

                var patients = await patientsQuery
                    .OrderBy(p => p.Name)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // 简化的导出格式 - CSV格式
                var csvContent = "姓名,性别,出生日期,手机号码,身份证号,地址,紧急联系人,紧急联系人电话,过敏史\n";
                
                foreach (var patient in patients)
                {
                    var gender = patient.Gender == Gender.Male ? "男" : "女";
                    csvContent += $"{patient.Name},{gender},{patient.BirthDate:yyyy-MM-dd}," +
                                 $"{patient.PhoneNumber},{patient.IdNumber},{patient.Address}," +
                                 $"{patient.EmergencyContactName},{patient.EmergencyContactPhone},{patient.AllergyHistory}\n";
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                
                _logger.LogInformation("导出患者数据成功 - 导出数量: {Count}", patients.Count);
                return ServiceResult<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<byte[]>.Failure($"导出患者数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证患者信息
        /// </summary>
        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                var validationResults = new List<string>();

                // 基础验证
                if (string.IsNullOrWhiteSpace(dto.Name))
                    validationResults.Add("姓名不能为空");

                if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
                    validationResults.Add("手机号码不能为空");
                else if (dto.PhoneNumber.Length != 11)
                    validationResults.Add("手机号码格式不正确");

                if (dto.BirthDate == default)
                    validationResults.Add("出生日期不能为空");
                else if (dto.BirthDate > DateTime.Today)
                    validationResults.Add("出生日期不能大于当前日期");

                // 检查重复
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
                {
                    var phoneExists = await _context.Patients
                        .AnyAsync(p => p.PhoneNumber == dto.PhoneNumber);
                    
                    if (phoneExists)
                        validationResults.Add("手机号码已存在");
                }

                if (!string.IsNullOrWhiteSpace(dto.IdNumber))
                {
                    var idExists = await _context.Patients
                        .AnyAsync(p => p.IdNumber == dto.IdNumber);
                    
                    if (idExists)
                        validationResults.Add("身份证号已存在");
                }

                var result = new
                {
                    IsValid = !validationResults.Any(),
                    Errors = validationResults
                };

                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者信息失败");
                return ServiceResult<object>.Failure($"验证患者信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量更新患者状态
        /// </summary>
        public async Task<ServiceResult<int>> BatchUpdateStatusAsync(List<Guid> ids, bool isEnabled)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (ids == null || !ids.Any())
                    return ServiceResult<int>.Failure("患者ID列表不能为空");

                var status = isEnabled ? CommonStatus.Enabled : CommonStatus.Disabled;
                var statusText = isEnabled ? "启用" : "禁用";

                var affectedRows = await _context.Patients
                    .Where(p => ids.Contains(p.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.Status, status)
                        .SetProperty(p => p.UpdateTime, DateTime.Now));

                await transaction.CommitAsync();

                _logger.LogInformation("批量{StatusText}患者成功 - 影响行数: {AffectedRows}", 
                    statusText, affectedRows);

                return ServiceResult<int>.Success(affectedRows);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "批量更新患者状态失败");
                return ServiceResult<int>.Failure($"批量更新患者状态失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清理非活跃患者（90天未就诊且无医疗案例的患者）
        /// </summary>
        public async Task<ServiceResult<int>> CleanupInactivePatientsAsync()
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-90);

                // 查找90天内没有医疗案例的患者
                var inactivePatientIds = await _context.Patients
                    .Where(p => p.Status == CommonStatus.Enabled)
                    .Where(p => !_context.MedicalCases.Any(mc => mc.PatientId == p.Id && mc.ConsultationDate >= cutoffDate))
                    .Select(p => p.Id)
                    .ToListAsync();

                if (!inactivePatientIds.Any())
                {
                    _logger.LogInformation("没有发现需要清理的非活跃患者");
                    return ServiceResult<int>.Success(0);
                }

                // 标记为非活跃状态（不删除，只是标记）
                var cleanedCount = await _context.Patients
                    .Where(p => inactivePatientIds.Contains(p.Id))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.DisableReason, p => (p.DisableReason ?? "") + " [系统清理：90天未活跃]")
                        .SetProperty(p => p.UpdateTime, DateTime.Now));

                _logger.LogInformation("清理非活跃患者完成 - 清理数量: {Count}", cleanedCount);
                return ServiceResult<int>.Success(cleanedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理非活跃患者失败");
                return ServiceResult<int>.Failure($"清理非活跃患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 生成患者导入模板
        /// </summary>
        public ServiceResult<byte[]> GenerateImportTemplate()
        {
            try
            {
                var templateContent = @"姓名,性别,出生日期,手机号码,身份证号,地址,紧急联系人,紧急联系人电话,备注
张三,男,1990-01-01,13800138001,110101199001011234,北京市朝阳区,李四,13800138002,示例数据
王五,女,1985-05-15,13800138003,110101198505151234,北京市海淀区,赵六,13800138004,示例数据";

                var bytes = System.Text.Encoding.UTF8.GetBytes(templateContent);
                
                _logger.LogInformation("生成患者导入模板成功");
                return ServiceResult<byte[]>.Success(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成患者导入模板失败");
                return ServiceResult<byte[]>.Failure($"生成患者导入模板失败: {ex.Message}");
            }
        }
    }
}