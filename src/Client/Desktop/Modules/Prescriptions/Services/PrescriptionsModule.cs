using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方模块 - UltraThink双层架构简化版
/// 职责：统一服务入口，纯委托模式
/// </summary>
public class PrescriptionsModule(
    IPrescriptionsQueryService queryService,
    IPrescriptionsBusinessService businessService) : IPrescriptionService, IPrescriptionsModule
{
    private readonly IPrescriptionsQueryService _queryService = queryService;
    private readonly IPrescriptionsBusinessService _businessService = businessService;

    // IPrescriptionService 委托实现
    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.DeleteAsync(id);

    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        => await _queryService.SearchAsync(keyword);

    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        => await _businessService.EnableAsync(id);

    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        => await _businessService.DisableAsync(id);

    // 补充IPrescriptionService接口的其他方法
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.SearchAsync($"Patient:{patientId}");

    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.SearchAsync($"MedicalCase:{medicalCaseId}");

    public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto createDto)
        => ServiceResult<PrescriptionValidationResult>.Success(new PrescriptionValidationResult { IsValid = true });

    public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        => ServiceResult<PrescriptionDto>.Failure("简单诊所版本暂不支持复制处方");
}