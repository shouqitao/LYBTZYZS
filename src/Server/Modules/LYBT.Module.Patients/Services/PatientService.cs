using FluentValidation;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Caching;
using LYBT.Infrastructure.Services;
using LYBT.Infrastructure.Services.CrossModule;
using System.Threading;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Mapping;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using GenericErrorCode = LYBT.Shared.Primitives.ErrorCodes.ErrorCode;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者服务 - 统一接口实现
    /// 包含DTO和Entity两种返回模式
    /// Phase 2: 继承BaseService<Patient>复用统一错误处理和验证逻辑
    /// OpenSpec: adopt-mapperly-unified-mapping - 使用PatientMapper替代AutoMapper
    /// Import/Export职责已拆分到PatientImportExportService，通过委托调用
    /// </summary>
    public class PatientService : BaseService<Patient>, IPatientService
    {
        private readonly IPatientRepository _repository;
        private readonly IValidator<PatientInputDto> _validator;
        private readonly PatientMapper _mapper = new();
        private readonly ICacheInvalidationService _cacheInvalidation;
        private readonly IPatientImportExportService _importExport;
        private readonly IMedicalCaseCrossModuleService _medicalCaseCrossModuleService;

        public PatientService(
            IPatientRepository repository,
            ILogger<PatientService> logger,
            IValidator<PatientInputDto> validator,
            ICacheInvalidationService cacheInvalidation,
            IPatientImportExportService importExport,
            IMedicalCaseCrossModuleService medicalCaseCrossModuleService)
            : base(logger)
        {
            _repository = repository;
            _validator = validator;
            _cacheInvalidation = cacheInvalidation;
            _importExport = importExport;
            _medicalCaseCrossModuleService = medicalCaseCrossModuleService;
        }

        public async Task<Result<PagedResult<PatientListDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，异常由IExceptionHandler统一处理
            // Bug #1587修复：支持关键字搜索（姓名/拼音码/手机号）
            // T5-P2-27: filterDisabled=true时只显示启用状态患者
            var pagedResult = filterDisabled
                ? await _repository.GetPagedWithStatusFilterAsync(page, pageSize, keyword, CommonStatus.Enabled)
                : await _repository.GetPagedAsync(page, pageSize, keyword);

            var items = _mapper.ToListDtos(pagedResult.Items.ToList());

            // 确保Age属性正确计算（从实体的计算属性复制到DTO）
            foreach (var item in items)
            {
                var entity = pagedResult.Items.FirstOrDefault(e => e.Id == item.Id);
                if (entity != null)
                {
                    item.Age = entity.Age;
                }
            }

            var dto = new PagedResult<PatientListDto>
            {
                Items = items,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = pagedResult.CurrentPage,
                PageSize = pagedResult.PageSize
            };
            return Result<PagedResult<PatientListDto>>.Success(dto);
        }

        /// <summary>
        /// 分页查询患者列表（返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<Result<PagedResult<PatientListDto>>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null, bool filterDisabled = false, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch
            // T5-P2-27: filterDisabled=true时只显示启用状态患者
            var pagedResult = filterDisabled
                ? await _repository.GetPagedWithStatusFilterAsync(page, pageSize, keyword, CommonStatus.Enabled)
                : await _repository.GetPagedAsync(page, pageSize, keyword);
            var dtos = _mapper.ToListDtos(pagedResult.Items.ToList());

            // 确保Age属性正确计算
            foreach (var dto in dtos)
            {
                var entity = pagedResult.Items.FirstOrDefault(e => e.Id == dto.Id);
                if (entity != null)
                {
                    dto.Age = entity.Age;
                }
            }

            var result = new PagedResult<PatientListDto>
            {
                Items = dtos,
                TotalCount = pagedResult.TotalCount,
                CurrentPage = page,
                PageSize = pageSize
            };
            return Result<PagedResult<PatientListDto>>.Success(result);
        }

        public async Task<Result<PatientDetailDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 业务逻辑检查保留在外部，无需ExecuteAsync包装
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            var dto = _mapper.ToDetailDto(entity);
            // 确保Age属性正确计算（从实体的计算属性复制到DTO）
            dto.Age = entity.Age;

            return Result<PatientDetailDto>.Success(dto);
        }

        public async Task<Result<PatientDetailDto>> CreateAsync(PatientInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务验证
            // FluentValidation 验证（Phase 1 Task 1.7）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogInformation("[SVC] Patient.Create → ValidationFailed - ErrorCount={ErrorCount} Errors={@Errors}", errors.Count, errors);
                return Result<PatientDetailDto>.Failure(errors);
            }

            // T5-P3-10: 检查手机号唯一性 (与 CreateEntityAsync 保持一致)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && !existingByPhone.IsDeleted)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已存在");
                }
            }

            // T5-P3-10: 检查身份证号唯一性 (与 CreateEntityAsync 保持一致)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已存在");
                }
            }

            var entity = _mapper.ToEntity(dto);

            // 生成拼音码（基于姓名）
            entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

            var result = await _repository.AddAsync(entity);
            await _cacheInvalidation.InvalidateAsync("patients");
            var resultDto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            resultDto.Age = result.Age;

            return Result<PatientDetailDto>.Success(resultDto);
        }

        public async Task<Result<PatientDetailDto>> UpdateAsync(Guid id, PatientInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            // FluentValidation 验证（Phase 1 Task 1.7）
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogInformation("[SVC] Patient.Update → ValidationFailed - PatientId={PatientId} ErrorCount={ErrorCount} Errors={@Errors}", id, errors.Count, errors);
                return Result<PatientDetailDto>.Failure(errors);
            }

            // T5-P2-25: 更新时检查手机号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && existingByPhone.Id != id)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已被其他患者使用");
                }
            }

            // T5-P2-26: 更新时检查身份证号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null && existingByIdNumber.Id != id)
                {
                    return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已被其他患者使用");
                }
            }

            // 保存旧的姓名用于检测变化
            var oldName = entity.Name;

            _mapper.UpdateEntity(dto, entity);

            // 更新拼音码（仅当姓名发生变化时）
            if (entity.Name != oldName)
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                _logger.LogDebug("[SVC] Patient.Update → PinYinRegenerated - OldName={OldName} NewName={NewName} PinYin={PinYin}",
                    oldName, entity.Name, entity.PinYinCode);
            }

            var result = await _repository.UpdateAsync(entity);
            await _cacheInvalidation.InvalidateAsync("patients");
            var resultDto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            resultDto.Age = result.Age;

            return Result<PatientDetailDto>.Success(resultDto);
        }

        public async Task<Result<List<PatientDetailDto>>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，修复ERR-012违规(ex.Message)
            // 如果关键字为空，返回空列表
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return Result<List<PatientDetailDto>>.Success(new List<PatientDetailDto>());
            }

            // Task 2.1: 优化搜索逻辑 - 使用Repository的GetPagedAsync方法避免全量加载
            // 搜索前100条匹配关键字的患者（姓名、电话或身份证号）
            var searchResult = await _repository.GetPagedAsync(1, 100, keyword);

            // 转换为DTO
            var patientDtos = _mapper.ToDetailDtos(searchResult.Items.ToList());

            return Result<List<PatientDetailDto>>.Success(patientDtos);
        }

        public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // X7: 删除前强制引用检查
            var refCheck = await CheckReferenceAsync(id);
            if (refCheck.IsSuccess && refCheck.Data != null && refCheck.Data.HasReferences)
            {
                _logger.LogWarning("[SVC] Patient.Delete → HasReferences - PatientId={PatientId} ReferenceCount={Count}",
                    id, refCheck.Data.ReferenceCount);
                return Result.Failure(GenericErrorCode.PatientHasActiveCases, $"患者有 {refCheck.Data.ReferenceCount} 条医案记录，无法删除");
            }

            var result = await _repository.DeleteAsync(id);
            if (result)
            {
                await _cacheInvalidation.InvalidateAsync("patients");
            }
            return result ? Result.Success() : Result.Failure(GenericErrorCode.InternalError, "删除失败");
        }

        // ========== Import/Export 职责委托给 IPatientImportExportService ==========

        /// <inheritdoc/>
        public Task<Result<PatientBatchImportResultDto>> BatchImportAsync(Stream stream, string? fileName = null, CancellationToken cancellationToken = default)
            => _importExport.BatchImportAsync(stream, fileName, cancellationToken);

        /// <inheritdoc/>
        public Task<MemoryStream> ExportTemplateAsync(ExportTemplateDto config, CancellationToken cancellationToken = default)
            => _importExport.ExportTemplateAsync(config, cancellationToken);

        /// <inheritdoc/>
        public Task<MemoryStream> ExportPatientsAsync(string? keyword = null, CancellationToken cancellationToken = default)
            => _importExport.ExportPatientsAsync(keyword, cancellationToken);

        #region IPatientServiceOptimized 实现 - Entity直接返回方法

        /// <summary>
        /// 获取分页患者数据（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除双重映射，提升性能15-20%
        /// </summary>
        public async Task<Result<PagedResult<Patient>>> GetPagedEntityAsync(int page = 1, int pageSize = 20, string? keyword = null, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch
            var pagedResult = await _repository.GetPagedAsync(page, pageSize, keyword);
            return Result<PagedResult<Patient>>.Success(pagedResult);
        }

        /// <summary>
        /// 根据ID获取患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除Entity→DTO映射，提升性能
        /// </summary>
        public async Task<Result<Patient>> GetByIdEntityAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务检查
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
                return Result<Patient>.Failure(GenericErrorCode.PatientNotFound);

            return Result<Patient>.Success(entity);
        }

        /// <summary>
        /// 创建患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> CreateEntityAsync(PatientInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务验证
            // FluentValidation 验证
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogInformation("[SVC] Patient.Create → ValidationFailed - ErrorCount={ErrorCount} Errors={@Errors}", errors.Count, errors);
                return Result<Patient>.Failure(errors);
            }

            // Issue #2245 Fix: 检查手机号唯一性(防止重复)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingPatient = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingPatient != null && !existingPatient.IsDeleted)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已存在");
                }
            }

            // T5-P2-24: 检查身份证号唯一性
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已存在");
                }
            }

            var entity = _mapper.ToEntity(dto);

            // 生成拼音码（基于姓名）
            entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);

            var result = await _repository.AddAsync(entity);
            return Result<Patient>.Success(result);
        }

        /// <summary>
        /// 更新患者（直接返回Patient Entity）
        /// Phase 3 Task 3.1: 消除DTO→Entity→DTO双重映射
        /// </summary>
        public async Task<Result<Patient>> UpdateEntityAsync(Guid id, PatientInputDto dto, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            // Issue #2245 Fix: 检查实体存在性(包括软删除状态)
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null || entity.IsDeleted)
                return Result<Patient>.Failure(GenericErrorCode.PatientNotFound);

            // FluentValidation 验证
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                _logger.LogInformation("[SVC] Patient.Update → ValidationFailed - PatientId={PatientId} ErrorCount={ErrorCount} Errors={@Errors}", id, errors.Count, errors);
                return Result<Patient>.Failure(errors);
            }

            // T5-P2-25: 更新时检查手机号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                var existingByPhone = await _repository.GetByPhoneNumberAsync(dto.PhoneNumber);
                if (existingByPhone != null && existingByPhone.Id != id)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientPhoneExists, $"手机号 {dto.PhoneNumber} 已被其他患者使用");
                }
            }

            // T5-P2-26: 更新时检查身份证号唯一性 (排除自身)
            if (!string.IsNullOrEmpty(dto.IdNumber))
            {
                var existingByIdNumber = await _repository.GetByIdNumberAsync(dto.IdNumber);
                if (existingByIdNumber != null && existingByIdNumber.Id != id)
                {
                    return Result<Patient>.Failure(GenericErrorCode.PatientIdCardExists, $"身份证号 {dto.IdNumber} 已被其他患者使用");
                }
            }

            // 保存旧的姓名用于检测变化
            var oldName = entity.Name;

            _mapper.UpdateEntity(dto, entity);

            // 更新拼音码（仅当姓名发生变化时）
            if (entity.Name != oldName)
            {
                entity.PinYinCode = PinYinHelper.GetPinYinCode(entity.Name);
                _logger.LogDebug("[SVC] Patient.Update → PinYinRegenerated - OldName={OldName} NewName={NewName} PinYin={PinYin}",
                    oldName, entity.Name, entity.PinYinCode);
            }

            var result = await _repository.UpdateAsync(entity);
            return Result<Patient>.Success(result);
        }

        #endregion

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法实现 ==========

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        public async Task<Result<PatientDetailDto>> ToggleStatusAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);
            }

            // CODE-22: 禁用患者前检查是否有未完成的医案 (Active/Suspended)
            // Architecture Fix: 使用IMedicalCaseCrossModuleService替代直接DbContext查询（解决循环依赖）
            if (entity.Status == CommonStatus.Enabled)
            {
                var unfinishedCount = await _medicalCaseCrossModuleService.CountUnfinishedMedicalCasesAsync(id);

                if (unfinishedCount > 0)
                {
                    return Result<PatientDetailDto>.Failure(
                        GenericErrorCode.PatientHasActiveCases,
                        $"该患者有 {unfinishedCount} 条进行中的医案，请先完成或取消后再禁用");
                }
            }

            // 切换状态
            entity.Status = entity.Status == CommonStatus.Enabled
                ? CommonStatus.Disabled
                : CommonStatus.Enabled;
            entity.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);
            dto.Age = result.Age;

            _logger.LogInformation("[SVC] Patient.ToggleStatus completed - PatientId={PatientId} Status={Status}", id, entity.Status);

            return Result<PatientDetailDto>.Success(dto);
        }

        /// <summary>
        /// 恢复软删除的患者
        /// </summary>
        public async Task<Result<PatientDetailDto>> RestoreAsync(Guid id, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 移除冗余try-catch，保留业务逻辑检查
            var entity = await _repository.GetByIdIncludingDeletedAsync(id);
            if (entity == null)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.PatientNotFound);

            if (!entity.IsDeleted)
                return Result<PatientDetailDto>.Failure(GenericErrorCode.InvalidPatientStatus, "该患者未被删除，无需恢复");

            entity.IsDeleted = false;
            entity.UpdatedAt = DateTime.UtcNow;

            var result = await _repository.UpdateAsync(entity);
            var dto = _mapper.ToDetailDto(result);

            // 确保Age属性正确计算
            dto.Age = result.Age;

            _logger.LogInformation("[SVC] Patient.Restore completed - PatientId={PatientId} Name={Name}", id, entity.Name);
            return Result<PatientDetailDto>.Success(dto);
        }

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <inheritdoc/>
        /// <remarks>
        /// eliminate-service-catch-return: 保留项级错误隔离，修复ERR-012违规
        /// </remarks>
        public async Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids, CancellationToken cancellationToken = default)
        {
            var result = new BatchOperationResultDto
            {
                TotalCount = ids.Count,
                SuccessCount = 0,
                FailureCount = 0
            };

            foreach (var id in ids)
            {
                try
                {
                    var entity = await _repository.GetByIdAsync(id);
                    if (entity == null)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = "患者不存在"
                        });
                        continue;
                    }

                    // X7: 批量删除逐个引用检查
                    // Architecture Fix: 使用IMedicalCaseCrossModuleService替代直接DbContext查询（解决循环依赖）
                    var refCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(id);
                    if (refCount > 0)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(id);
                        result.FailedItems.Add(new BatchOperationFailureItem
                        {
                            Id = id,
                            Reason = $"患者有 {refCount} 条医案记录，无法删除"
                        });
                        continue;
                    }

                    // 软删除
                    entity.IsDeleted = true;
                    entity.UpdatedAt = DateTime.UtcNow;
                    await _repository.UpdateAsync(entity);

                    result.SuccessCount++;
                    result.SuccessfulIds.Add(id);
                    _logger.LogInformation("[SVC] Patient.BatchDelete → ItemSuccess - PatientId={PatientId} Name={Name}", id, entity.Name);
                }
                catch (Exception ex)
                {
                    // 项级错误隔离：单项失败不影响其他项
                    // ERR-012: 使用安全消息替代ex.Message
                    result.FailureCount++;
                    result.FailedIds.Add(id);
                    result.FailedItems.Add(new BatchOperationFailureItem
                    {
                        Id = id,
                        Reason = "删除操作失败"
                    });
                    _logger.LogError(ex, "[SVC] Patient.BatchDelete → ItemFailed - PatientId={PatientId}", id);
                }
            }

            result.IsSuccess = result.SuccessCount > 0;
            result.Message = $"批量删除完成：成功 {result.SuccessCount} 条，失败 {result.FailureCount} 条";

            return Result<BatchOperationResultDto>.Success(result);
        }

        // ========== OpenSpec: implement-data-sync - 引用检查 ==========

        /// <summary>
        /// 检查患者是否被医案引用
        /// </summary>
        public async Task<Result<PatientReferenceCheckDto>> CheckReferenceAsync(Guid patientId, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理
            var patient = await _repository.GetByIdAsync(patientId);
            if (patient == null)
            {
                return Result<PatientReferenceCheckDto>.Failure(GenericErrorCode.PatientNotFound);
            }

            // Architecture Fix: 使用IMedicalCaseCrossModuleService替代直接DbContext查询（解决循环依赖）
            // 查询医案引用计数
            var referenceCount = await _medicalCaseCrossModuleService.CountMedicalCasesAsync(patientId);

            // 获取最近5条引用记录
            var recentMedicalCases = await _medicalCaseCrossModuleService.GetRecentMedicalCasesAsync(patientId, 5);

            var hasReferences = referenceCount > 0;
            var result = new PatientReferenceCheckDto
            {
                PatientId = patientId,
                PatientName = patient.Name,
                HasReferences = hasReferences,
                ReferenceCount = referenceCount,
                CanDelete = !hasReferences, // X7: 有引用不可删除
                DeleteWarning = hasReferences ? $"该患者已有 {referenceCount} 个医案记录，无法删除" : null,
                RecentMedicalCases = recentMedicalCases
            };

            _logger.LogInformation("[SVC] Patient.CheckReference completed - PatientName={PatientName} HasReferences={HasReferences} ReferenceCount={ReferenceCount}",
                patient.Name, hasReferences, referenceCount);

            return Result<PatientReferenceCheckDto>.Success(result);
        }

        /// <summary>
        /// 批量检查患者引用关系
        /// </summary>
        public async Task<Result<List<PatientReferenceCheckDto>>> BatchCheckReferenceAsync(List<Guid> patientIds, CancellationToken cancellationToken = default)
        {
            // eliminate-service-catch-return: 异常由IExceptionHandler统一处理
            const int MAX_CHECK_SIZE = 100;

            // 批量检查数量限制
            if (patientIds.Count > MAX_CHECK_SIZE)
            {
                return Result<List<PatientReferenceCheckDto>>.Failure(GenericErrorCode.ValidationFailed, $"批量检查最多支持{MAX_CHECK_SIZE}条记录");
            }

            var results = new List<PatientReferenceCheckDto>();

            foreach (var patientId in patientIds)
            {
                var checkResult = await CheckReferenceAsync(patientId);
                if (checkResult.IsSuccess && checkResult.Data != null)
                {
                    results.Add(checkResult.Data);
                }
            }

            _logger.LogInformation("[SVC] Patient.BatchCheckReference completed - Count={Count}", results.Count);

            return Result<List<PatientReferenceCheckDto>>.Success(results);
        }
    }
}
