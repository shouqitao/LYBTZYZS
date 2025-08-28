using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Helpers;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Module.Prescriptions.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services.Core
{
    /// <summary>
    /// 处方CRUD服务实现 - UltraThink重构版本
    /// 负责处方的基础增删改查操作
    /// </summary>
    public class PrescriptionCrudService : IPrescriptionCrudService
    {
        private readonly IPrescriptionRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly IIntelligentPrescriptionService _intelligentService;
        private readonly PrescriptionValidationHelper _validationHelper;
        private readonly ILogger<PrescriptionCrudService> _logger;

        public PrescriptionCrudService(
            IPrescriptionRepository repository,
            AppDbContext dbContext,
            IMapper mapper,
            IIntelligentPrescriptionService intelligentService,
            PrescriptionValidationHelper validationHelper,
            ILogger<PrescriptionCrudService> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _intelligentService = intelligentService ?? throw new ArgumentNullException(nameof(intelligentService));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 创建新处方 - 医生开具处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto, Guid operatorId, string operatorName)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                // 验证创建请求
                var validationResult = await _validationHelper.ValidateCreateAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<PrescriptionDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
                }

                if (!validationResult.Data.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Data.Errors);
                    return ServiceResult<PrescriptionDto>.Failure($"请求验证失败: {errors}");
                }

                // 映射创建实体
                var model = _mapper.Map<LYBT.Entities.Prescriptions.Prescription>(dto);
                model.Id = Guid.NewGuid();
                model.Status = PrescriptionStatus.Draft; // 默认为草稿状态

                // 执行智能检查 - 简化版本
                if (dto.Items != null && dto.Items.Any())
                {
                    var itemDtos = dto.Items.Select(item => new PrescriptionItemDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        Price = item.UnitPrice,
                        Usage = item.Usage,
                        Remark = item.Remark ?? item.Note
                    }).ToList();

                    var duplicateResult = _intelligentService.DetectDuplicateHerbs(itemDtos);
                    if (duplicateResult.IsSuccess && duplicateResult.Data.Count < itemDtos.Count)
                    {
                        _logger.LogWarning("处方重复药材警告 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}, 原始数量: {Original}, 去重后数量: {Deduplicated}",
                            operatorName, model.Id, itemDtos.Count, duplicateResult.Data.Count);
                    }
                }

                // 保存到数据库
                var success = await _repository.AddAsync(model);
                if (!success)
                {
                    return ServiceResult<PrescriptionDto>.Failure("保存处方失败");
                }

                // 如果处方关联了医疗案例，更新案例状态
                if (dto.ConsultationId.HasValue)
                {
                    try
                    {
                        var medicalCase = await _dbContext.MedicalCases.FindAsync(dto.ConsultationId.Value);
                        if (medicalCase != null)
                        {
                            medicalCase.Remark = string.IsNullOrEmpty(medicalCase.Remark) 
                                ? "处方已创建" 
                                : $"{medicalCase.Remark}\n处方已创建";                    
                            _dbContext.MedicalCases.Update(medicalCase);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "更新医疗案例状态失败 - 案例ID: {CaseId}", dto.ConsultationId.Value);
                    }
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                // 记录操作日志
                _logger.LogInformation("处方新增 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, model.Id);

                // 返回创建的DTO
                var resultDto = _mapper.Map<PrescriptionDto>(model);
                return ServiceResult<PrescriptionDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "创建处方失败 - 操作者: {OperatorName}", operatorName);
                return ServiceResult<PrescriptionDto>.Failure("创建处方失败");
            }
        }

        /// <summary>
        /// 更新处方
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateAsync(PrescriptionEditDto dto, Guid operatorId, string operatorName)
        {
            try
            {
                // 验证更新请求
                var validationResult = await _validationHelper.ValidateUpdateAsync(dto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(validationResult.ErrorMessage ?? "验证失败");
                }

                if (!validationResult.Data.IsValid)
                {
                    var errors = string.Join("; ", validationResult.Data.Errors);
                    return ServiceResult<bool>.Failure($"请求验证失败: {errors}");
                }

                // 检查现有处方
                var existingPrescription = await _repository.GetByIdAsync(dto.Id);
                if (existingPrescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以编辑
                var canEditResult = _validationHelper.ValidateCanEdit(existingPrescription.Status);
                if (!canEditResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canEditResult.ErrorMessage ?? "无法编辑处方");
                }

                // 映射更新实体 - 使用AutoMapper确保实体更新完整性
                var updatedModel = _mapper.Map(dto, existingPrescription);

                // 保存更新
                var success = await _repository.UpdateAsync(updatedModel);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("更新处方失败");
                }

                // 记录操作日志
                _logger.LogInformation("处方编辑 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, dto.Id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, dto.Id);
                return ServiceResult<bool>.Failure("更新处方失败");
            }
        }

        /// <summary>
        /// 删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            try
            {
                // 检查处方
                var prescription = await _repository.GetByIdAsync(id);
                if (prescription == null)
                {
                    return ServiceResult<bool>.Failure("处方不存在");
                }

                // 验证是否可以删除
                var canDeleteResult = _validationHelper.ValidateCanDelete(prescription.Status);
                if (!canDeleteResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(canDeleteResult.ErrorMessage ?? "无法删除处方");
                }

                // 执行删除
                var success = await _repository.DeleteAsync(id);
                if (!success)
                {
                    return ServiceResult<bool>.Failure("删除处方失败");
                }

                // 记录操作日志
                _logger.LogInformation("处方删除 - 操作者: {OperatorName} ({OperatorId}), 处方ID: {PrescriptionId}",
                    operatorName, operatorId, id);

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败 - 操作者: {OperatorName}, 处方ID: {PrescriptionId}", operatorName, id);
                return ServiceResult<bool>.Failure("删除处方失败");
            }
        }
    }
}