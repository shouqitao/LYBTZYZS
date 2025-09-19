using AutoMapper;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Patients.Services
{

    /// <summary>
    /// 患者业务服务实现
    /// UltraThink架构 - Business层接口抽象
    /// 职责：患者业务逻辑、CRUD操作、状态管理
    /// </summary>
    public class PatientBusinessService : IPatientBusinessService
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
        /// 创建患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto createDto, CancellationToken cancellationToken = default)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 数据验证
                    if (createDto == null)
                    {
                        return ServiceResult<PatientDto>.Failure("患者信息不能为空");
                    }

                    if (string.IsNullOrWhiteSpace(createDto.Name))
                    {
                        return ServiceResult<PatientDto>.Failure("姓名不能为空");
                    }

                    // 检查重复手机号
                    if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                    {
                        var phoneExists = await _context.Patients
                            .AnyAsync(p => p.PhoneNumber == createDto.PhoneNumber, cancellationToken);

                        if (phoneExists)
                        {
                            return ServiceResult<PatientDto>.Failure("手机号码已存在");
                        }
                    }

                    // 创建新患者
                    var patient = _mapper.Map<Patient>(createDto);
                    patient.Id = Guid.NewGuid();
                    patient.Status = CommonStatus.Enabled;
                    patient.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现
                    patient.CreatedAt = DateTime.Now;

                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("创建患者成功: {Name} ({Id})", patient.Name, patient.Id);

                    var resultDto = _mapper.Map<PatientDto>(patient);
                    return ServiceResult<PatientDto>.Success(resultDto);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    _logger.LogError(ex, "创建患者失败: {Name}", createDto?.Name);
                    return ServiceResult<PatientDto>.Failure($"创建患者失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid patientId, PatientUpdateDto updateDto)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }

                if (updateDto == null)
                {
                    return ServiceResult<PatientDto>.Failure("更新信息不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                // 检查手机号重复（排除自己）
                if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                {
                    var phoneExists = await _context.Patients
                        .AnyAsync(p => p.PhoneNumber == updateDto.PhoneNumber && p.Id != patientId);

                    if (phoneExists)
                    {
                        return ServiceResult<PatientDto>.Failure("手机号码已存在");
                    }
                }

                // 更新字段
                _mapper.Map(updateDto, patient);
                patient.PinYinCode = string.Empty; // 移除CommonHelper依赖，拼音码功能暂不实现
                patient.UpdateTime = DateTime.Now;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新患者成功: {Name} ({Id})", patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "患者数据并发冲突: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure("数据已被其他用户修改，请刷新后重试");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者失败: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure($"更新患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        public async Task<ServiceResult<PatientDto>> DeleteAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<PatientDto>.Failure("患者ID不能为空");
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.Id == patientId);

                if (patient == null)
                {
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                }

                // 软删除 - 标记为已删除状态
                patient.Status = CommonStatus.Disabled;
                patient.UpdateTime = DateTime.Now;

                _context.Patients.Update(patient);
                await _context.SaveChangesAsync();

                _logger.LogInformation("删除患者成功: {Name} ({Id})", patient.Name, patient.Id);

                var resultDto = _mapper.Map<PatientDto>(patient);
                return ServiceResult<PatientDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败: {Id}", patientId);
                return ServiceResult<PatientDto>.Failure($"删除患者失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(List<Guid> patientIds)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (patientIds == null || !patientIds.Any())
                    {
                        return ServiceResult<bool>.Failure("患者ID列表不能为空");
                    }

                    var affectedRows = await _context.Patients
                        .Where(p => patientIds.Contains(p.Id))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Status, CommonStatus.Disabled)
                            .SetProperty(p => p.UpdateTime, DateTime.Now));

                    await transaction.CommitAsync();

                    _logger.LogInformation("批量删除患者成功 - 影响行数: {AffectedRows}", affectedRows);
                    return ServiceResult<bool>.Success(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量删除患者失败");
                    return ServiceResult<bool>.Failure($"批量删除患者失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 设置患者状态
        /// </summary>
        public async Task<ServiceResult<bool>> SetStatusAsync(List<Guid> patientIds, string status)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (patientIds == null || !patientIds.Any())
                    {
                        return ServiceResult<bool>.Failure("患者ID列表不能为空");
                    }

                    var commonStatus = status.ToLower() == "enabled" ? CommonStatus.Enabled : CommonStatus.Disabled;

                    var affectedRows = await _context.Patients
                        .Where(p => patientIds.Contains(p.Id))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Status, commonStatus)
                            .SetProperty(p => p.UpdateTime, DateTime.Now));

                    await transaction.CommitAsync();

                    _logger.LogInformation("批量设置患者状态成功 - 状态: {Status}, 影响行数: {AffectedRows}", status, affectedRows);
                    return ServiceResult<bool>.Success(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量设置患者状态失败");
                    return ServiceResult<bool>.Failure($"批量设置患者状态失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 启用患者
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(List<Guid> patientIds)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (patientIds == null || !patientIds.Any())
                    {
                        return ServiceResult<bool>.Failure("患者ID列表不能为空");
                    }

                    var affectedRows = await _context.Patients
                        .Where(p => patientIds.Contains(p.Id))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Status, CommonStatus.Enabled)
                            .SetProperty(p => p.UpdateTime, DateTime.Now));

                    await transaction.CommitAsync();

                    _logger.LogInformation("批量启用患者成功 - 影响行数: {AffectedRows}", affectedRows);
                    return ServiceResult<bool>.Success(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量启用患者失败");
                    return ServiceResult<bool>.Failure($"批量启用患者失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 禁用患者
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(List<Guid> patientIds)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (patientIds == null || !patientIds.Any())
                    {
                        return ServiceResult<bool>.Failure("患者ID列表不能为空");
                    }

                    // 检查是否有活跃的医疗案例
                    var hasActiveCases = await _context.MedicalCases
                        .AnyAsync(mc => patientIds.Contains(mc.PatientId) && mc.Status != MedicalCaseStatus.Completed);

                    if (hasActiveCases)
                    {
                        return ServiceResult<bool>.Failure("部分患者有活跃的医疗案例，无法禁用");
                    }

                    var affectedRows = await _context.Patients
                        .Where(p => patientIds.Contains(p.Id))
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(p => p.Status, CommonStatus.Disabled)
                            .SetProperty(p => p.UpdateTime, DateTime.Now));

                    await transaction.CommitAsync();

                    _logger.LogInformation("批量禁用患者成功 - 影响行数: {AffectedRows}", affectedRows);
                    return ServiceResult<bool>.Success(true);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批量禁用患者失败");
                    return ServiceResult<bool>.Failure($"批量禁用患者失败: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 导入患者数据 - Phase C1 事务优化：50条/批短事务模式
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ImportPatientsAsync(List<PatientImportDto> importDtos)
        {
            try
            {
                if (importDtos == null || !importDtos.Any())
                {
                    return ServiceResult<List<PatientDto>>.Failure("导入数据不能为空");
                }

                const int BATCH_SIZE = 50; // Phase C1: 小诊所优化，50条/批减少事务时间
                var allSuccessfulPatients = new List<PatientDto>();
                var totalErrors = new List<string>();
                var batches = SplitIntoBatches(importDtos, BATCH_SIZE);

                _logger.LogInformation("开始分批导入患者 - 总数: {Total}, 批次数: {BatchCount}, 每批: {BatchSize}条", 
                    importDtos.Count, batches.Count, BATCH_SIZE);

                // 分批处理，每批使用独立的短事务
                for (int batchIndex = 0; batchIndex < batches.Count; batchIndex++)
                {
                    var batch = batches[batchIndex];
                    var batchResult = await ImportPatientsBatch(batch, batchIndex + 1, BATCH_SIZE);
                    
                    allSuccessfulPatients.AddRange(batchResult.SuccessfulPatients);
                    totalErrors.AddRange(batchResult.Errors);
                }

                _logger.LogInformation(
                    "患者批量导入完成 - 成功: {SuccessCount}, 失败: {ErrorCount}, 批次: {BatchCount}",
                    allSuccessfulPatients.Count, totalErrors.Count, batches.Count);

                if (totalErrors.Count > 0 && allSuccessfulPatients.Count == 0)
                {
                    return ServiceResult<List<PatientDto>>.Failure($"导入失败，所有记录都有错误：{string.Join("; ", totalErrors.Take(5))}");
                }

                return ServiceResult<List<PatientDto>>.Success(allSuccessfulPatients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者异常");
                return ServiceResult<List<PatientDto>>.Failure($"批量导入患者异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入单个批次的患者 - Phase C1 短事务实现
        /// </summary>
        private async Task<(List<PatientDto> SuccessfulPatients, List<string> Errors)> ImportPatientsBatch(
            List<PatientImportDto> batch, int batchNumber, int batchSize)
        {
            return await _context.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var successfulPatients = new List<PatientDto>();
                    var errors = new List<string>();
                    var baseIndex = (batchNumber - 1) * batchSize;

                    foreach (var (importDto, index) in batch.Select((dto, i) => (dto, i)))
                    {
                        try
                        {
                            var rowNumber = baseIndex + index + 1;

                            // 检查重复手机号
                            if (!string.IsNullOrEmpty(importDto.PhoneNumber))
                            {
                                var existingPatient = await _context.Patients
                                    .AnyAsync(p => p.PhoneNumber == importDto.PhoneNumber);

                                if (existingPatient)
                                {
                                    errors.Add($"行 {rowNumber}: 患者 {importDto.Name} 手机号 {importDto.PhoneNumber} 已存在");
                                    continue;
                                }
                            }

                            // 解析性别
                            var gender = importDto.GenderText.ToLower() switch
                            {
                                "男" or "male" => Gender.Male,
                                "女" or "female" => Gender.Female,
                                _ => Gender.Male // 默认值
                            };

                            // 解析出生日期
                            DateTime birthDate = DateTime.Today.AddYears(-30); // 默认30岁
                            if (!string.IsNullOrEmpty(importDto.BirthDateText))
                            {
                                DateTime.TryParse(importDto.BirthDateText, out birthDate);
                            }
                            else if (importDto.Age.HasValue)
                            {
                                birthDate = DateTime.Today.AddYears(-importDto.Age.Value);
                            }

                            var patient = new Patient
                            {
                                Id = Guid.NewGuid(),
                                Name = importDto.Name,
                                Gender = gender,
                                BirthDate = birthDate,
                                PhoneNumber = importDto.PhoneNumber,
                                IdNumber = importDto.IdCardNumber,
                                Address = importDto.Address,
                                EmergencyContactName = importDto.EmergencyContact,
                                EmergencyContactPhone = importDto.EmergencyPhone,
                                AllergyHistory = importDto.AllergyHistory,
                                Status = CommonStatus.Enabled,
                                PinYinCode = string.Empty, // 移除CommonHelper依赖，拼音码功能暂不实现
                                CreatedAt = DateTime.Now
                            };

                            _context.Patients.Add(patient);
                            var patientDto = _mapper.Map<PatientDto>(patient);
                            successfulPatients.Add(patientDto);
                        }
                        catch (Exception ex)
                        {
                            var rowNumber = baseIndex + index + 1;
                            errors.Add($"行 {rowNumber}: 患者 {importDto.Name} 导入失败: {ex.Message}");
                            _logger.LogError(ex, "导入患者失败: {Name}, 批次: {BatchNumber}", importDto.Name, batchNumber);
                        }
                    }

                    if (successfulPatients.Count > 0)
                    {
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        _logger.LogInformation("批次 {BatchNumber} 导入患者成功: {ImportCount}条", batchNumber, successfulPatients.Count);
                    }
                    else
                    {
                        await transaction.RollbackAsync();
                        _logger.LogWarning("批次 {BatchNumber} 没有成功导入任何患者", batchNumber);
                    }

                    return (SuccessfulPatients: successfulPatients, Errors: errors);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(ex, "批次 {BatchNumber} 导入患者并发冲突", batchNumber);
                    return (SuccessfulPatients: new List<PatientDto>(), 
                           Errors: new List<string> { $"批次 {batchNumber}: 数据已被其他用户修改，请重试" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "批次 {BatchNumber} 导入患者异常", batchNumber);
                    return (SuccessfulPatients: new List<PatientDto>(), 
                           Errors: new List<string> { $"批次 {batchNumber}: 导入异常 - {ex.Message}" });
                }
            });
        }

        /// <summary>
        /// 将列表拆分为指定大小的批次 - Phase C1 辅助方法
        /// </summary>
        private static List<List<T>> SplitIntoBatches<T>(List<T> items, int batchSize)
        {
            var batches = new List<List<T>>();
            for (int i = 0; i < items.Count; i += batchSize)
            {
                var batch = items.Skip(i).Take(batchSize).ToList();
                batches.Add(batch);
            }
            return batches;
        }

        /// <summary>
        /// 导出患者数据
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync(PatientExportDto exportDto)
        {
            try
            {
                var patientsQuery = _context.Patients.AsQueryable();

                // 应用导出筛选条件
                if (!string.IsNullOrWhiteSpace(exportDto.Name))
                {
                    patientsQuery = patientsQuery.Where(p => p.Name.Contains(exportDto.Name));
                }

                if (!string.IsNullOrWhiteSpace(exportDto.PhoneNumber))
                {
                    patientsQuery = patientsQuery.Where(p => p.PhoneNumber != null && p.PhoneNumber.Contains(exportDto.PhoneNumber));
                }

                var patients = await patientsQuery
                    .OrderBy(p => p.Name)
                    .ToListAsync();

                var patientDtos = _mapper.Map<List<PatientDto>>(patients);

                _logger.LogInformation("导出患者数据成功 - 导出数量: {Count}", patientDtos.Count);
                return ServiceResult<List<PatientDto>>.Success(patientDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<List<PatientDto>>.Failure($"导出患者数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 验证患者数据
        /// </summary>
        public async Task<ServiceResult<List<string>>> ValidatePatientAsync(PatientCreateDto createDto)
        {
            try
            {
                var validationResults = new List<string>();

                // 基础验证
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    validationResults.Add("姓名不能为空");
                }

                if (string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    validationResults.Add("手机号码不能为空");
                }
                else if (createDto.PhoneNumber.Length != 11)
                {
                    validationResults.Add("手机号码格式不正确");
                }

                if (createDto.BirthDate == default)
                {
                    validationResults.Add("出生日期不能为空");
                }
                else if (createDto.BirthDate > DateTime.Today)
                {
                    validationResults.Add("出生日期不能大于当前日期");
                }

                // 检查重复
                if (!string.IsNullOrWhiteSpace(createDto.PhoneNumber))
                {
                    var phoneExists = await _context.Patients
                        .AnyAsync(p => p.PhoneNumber == createDto.PhoneNumber);

                    if (phoneExists)
                    {
                        validationResults.Add("手机号码已存在");
                    }
                }

                if (!string.IsNullOrWhiteSpace(createDto.IdNumber))
                {
                    var idExists = await _context.Patients
                        .AnyAsync(p => p.IdNumber == createDto.IdNumber);

                    if (idExists)
                    {
                        validationResults.Add("身份证号已存在");
                    }
                }

                return ServiceResult<List<string>>.Success(validationResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者信息失败");
                return ServiceResult<List<string>>.Failure($"验证患者信息失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取导入模板
        /// </summary>
        public Task<ServiceResult<object>> GetImportTemplate()
        {
            try
            {
                var template = new
                {
                    Headers = new[] { "姓名", "性别", "出生日期", "手机号码", "身份证号", "地址", "紧急联系人", "紧急联系人电话", "过敏史" },
                    SampleData = new[]
                    {
                        new { 姓名 = "张三", 性别 = "男", 出生日期 = "1990-01-01", 手机号码 = "13800138001", 身份证号 = "110101199001011234", 地址 = "北京市朝阳区", 紧急联系人 = "李四", 紧急联系人电话 = "13800138002", 过敏史 = "无" },
                        new { 姓名 = "王五", 性别 = "女", 出生日期 = "1985-05-15", 手机号码 = "13800138003", 身份证号 = "110101198505151234", 地址 = "北京市海淀区", 紧急联系人 = "赵六", 紧急联系人电话 = "13800138004", 过敏史 = "青霉素" }
                    }
                };

                _logger.LogInformation("获取患者导入模板成功");
                return Task.FromResult(ServiceResult<object>.Success(template));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者导入模板失败");
                return Task.FromResult(ServiceResult<object>.Failure($"获取患者导入模板失败: {ex.Message}"));
            }
        }
    }
}
