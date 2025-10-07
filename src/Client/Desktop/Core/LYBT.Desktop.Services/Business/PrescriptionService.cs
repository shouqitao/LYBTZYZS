using AutoMapper;
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
        private readonly IMapper _mapper;

        public PrescriptionService(
            IPrescriptionRepository repository,
            ILogger<PrescriptionService> logger,
            IExceptionHandler exceptionHandler,
            IMapper mapper)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
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

                // 使用 AutoMapper 转换 DTO
                var prescription = _mapper.Map<PrescriptionDto>(dto);
                prescription.Id = Guid.NewGuid();
                prescription.Items = new List<PrescriptionItemDto>(); // Items 集合在 Profile 中 Ignore,需手动初始化

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

                // 手动映射 PrescriptionUpdateDto → PrescriptionDto (只映射共同字段)
                // PrescriptionDto 不包含: PrescriptionNumber, Diagnosis
                existing.Advice = dto.Advice;
                existing.DosageCount = dto.DosageCount;
                existing.Usage = dto.Usage;
                existing.Discount = dto.Discount;
                existing.Remark = dto.Remark;
                existing.UpdatedAt = DateTime.UtcNow;

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
