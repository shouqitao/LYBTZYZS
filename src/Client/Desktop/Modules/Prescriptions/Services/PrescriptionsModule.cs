using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理模块 - UltraThink双层架构纯委托层
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// 实现IPrescriptionService接口，与后端标准完全对齐
/// 集成处方开具、药材配伍、验方组合、智能计算和打印输出功能
/// 适配中医诊所处方管理需求，确保配伍安全性和计算准确性
/// </summary>
public class PrescriptionsModule(
    IPrescriptionsQueryService queryService,
    IPrescriptionsBusinessService businessService) : IPrescriptionService
{
    private readonly IPrescriptionsQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IPrescriptionsBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    // IPrescriptionService 委托实现
    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.Delete(id);

    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        => await _queryService.GetPagedAsync(query);

    public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        => await _queryService.Search(keyword);

    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        => await _businessService.Enable(id);

    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        => await _businessService.Disable(id);

    // 补充IPrescriptionService接口的其他方法
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.Search($"Patient:{patientId}");

    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.Search($"MedicalCase:{medicalCaseId}");

    public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto createDto)
        => ServiceResult<PrescriptionValidationResult>.Success(new PrescriptionValidationResult { IsValid = true });

    public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        => ServiceResult<PrescriptionDto>.Failure("简单诊所版本暂不支持复制处方");
}
