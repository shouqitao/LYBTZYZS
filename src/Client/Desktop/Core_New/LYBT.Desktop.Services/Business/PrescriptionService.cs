using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Services.Exceptions;
using LYBT.Desktop.Services.Repositories.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 处方服务实现 - UltraThink架构
    /// 实现Shared.Interfaces统一接口，返回ServiceResult包装
    /// </summary>
    public class PrescriptionService : IPrescriptionService
    {
        private readonly ILogger<PrescriptionService> _logger;
        private readonly IPrescriptionRepository _repository;
        private readonly IExceptionHandler _exceptionHandler;

        public PrescriptionService(
            IPrescriptionRepository repository,
            ILogger<PrescriptionService> logger,
            IExceptionHandler exceptionHandler)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        }

        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var allPrescriptions = await _repository.GetAllAsync();

                // 应用关键词搜索
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    allPrescriptions = allPrescriptions.Where(p =>
                        (p.Remark != null && p.Remark.Contains(keyword, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Indication != null && p.Indication.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                // 分页
                var totalCount = allPrescriptions.Count;
                var items = allPrescriptions
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedResult = new PagedResult<PrescriptionDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                return ServiceResult<PagedResult<PrescriptionDto>>.Success(pagedResult);
            }, nameof(GetPagedAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                var prescription = await _repository.GetByIdAsync(id);
                return ServiceResult<PrescriptionDto>.Success(prescription);
            }, nameof(GetByIdAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                _logger.LogInformation($"创建处方: 患者ID={dto.PatientId}");

                // 转换DTO
                var prescription = new PrescriptionDto
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    MedicalCaseId = Guid.Empty, // 需要关联具体医案
                    UserId = dto.DoctorId,
                    DosageCount = dto.Quantity,
                    Usage = dto.Usage,
                    Remark = dto.Notes,
                    FormulaSource = dto.FormulaSource,
                    Items = new List<PrescriptionItemDto>(),
                    Status = CommonStatus.Enabled,
                    CreateTime = DateTime.UtcNow,
                    UpdateTime = DateTime.UtcNow
                };

                var created = await _repository.CreateAsync(prescription);
                return ServiceResult<PrescriptionDto>.Success(created);
            }, nameof(CreateAsync));
        }

        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // 先获取现有数据
                var existing = await _repository.GetByIdAsync(id);

                // 更新字段
                existing.DosageCount = dto.DosageCount;
                existing.Advice = dto.Advice;
                existing.Remark = dto.Remark;
                existing.UpdateTime = DateTime.UtcNow;

                // 处理Items更新
                if (dto.Items != null && dto.Items.Any())
                {
                    // TODO: 实现Items的更新逻辑
                    _logger.LogWarning("PrescriptionUpdateDto.Items更新逻辑待实现");
                }

                var updated = await _repository.UpdateAsync(existing);
                return ServiceResult<PrescriptionDto>.Success(updated);
            }, nameof(UpdateAsync));
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                await _repository.DeleteAsync(id);
                return ServiceResult.Success();
            }, nameof(DeleteAsync));
        }

        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _exceptionHandler.SafeExecuteAsync(async () =>
            {
                // Repository需要扩展此方法，当前使用GetAll + 过滤
                var allPrescriptions = await _repository.GetAllAsync();
                var prescriptions = allPrescriptions
                    .Where(p => p.MedicalCaseId == medicalCaseId)
                    .ToList();

                return ServiceResult<List<PrescriptionDto>>.Success(prescriptions);
            }, nameof(GetByMedicalCaseIdAsync));
        }
    }
}