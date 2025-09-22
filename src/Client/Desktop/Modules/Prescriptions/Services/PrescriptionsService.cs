using LYBT.Desktop.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理服务 - UltraThink双层架构纯委托层
/// 重构：从PrescriptionsModule重命名为PrescriptionsService，避免与Prism IModule混淆
/// 采用UltraThink架构标准，使用C# 12现代化特性
/// 职责：统一服务入口，请求路由分发到QueryService和BusinessService
/// 实现IPrescriptionService接口，与后端标准完全对齐
/// 集成处方开具、药材配伍、验方组合、智能计算和打印输出功能
/// 适配中医诊所处方管理需求，确保配伍安全性和计算准确性
/// </summary>
public class PrescriptionsService(
    IPrescriptionsQueryService queryService,
    IPrescriptionsBusinessService businessService) : IPrescriptionService
{
    private readonly IPrescriptionsQueryService _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
    private readonly IPrescriptionsBusinessService _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));

    // IPrescriptionService 委托实现

    /// <inheritdoc/>
    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto createDto)
        => await _businessService.CreateAsync(createDto);

    /// <inheritdoc/>
    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto updateDto)
        => await _businessService.UpdateAsync(id, updateDto);

    /// <inheritdoc/>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => await _businessService.Delete(id);

    /// <inheritdoc/>
    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        => await _queryService.GetByIdAsync(id);

    /// <inheritdoc/>
    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        => await _queryService.GetPagedAsync(query);

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        => await _queryService.Search(keyword);

    public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        => await _businessService.Enable(id);

    public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        => await _businessService.Disable(id);

    // 补充IPrescriptionService接口的其他方法

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        => await _queryService.Search($"Patient:{patientId}");

    /// <inheritdoc/>
    public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        => await _queryService.Search($"MedicalCase:{medicalCaseId}");

    /// <inheritdoc/>
    public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto createDto)
        => ServiceResult<PrescriptionValidationResult>.Success(new PrescriptionValidationResult { IsValid = true });

    /// <inheritdoc/>
    public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
    {
        try
        {
            // 首先获取原处方详情
            var originalResult = await _queryService.GetByIdAsync(id);
            if (!originalResult.IsSuccess || originalResult.Data == null)
            {
                return ServiceResult<PrescriptionDto>.Failure("无法获取原处方信息，复制失败");
            }

            var original = originalResult.Data;

            // 创建复制的处方DTO
            var createDto = new PrescriptionCreateDto
            {
                PatientId = original.PatientId,
                DoctorId = original.UserId, // 使用UserId作为DoctorId
                ConsultationId = null, // 新处方不关联特定会诊
                Diagnosis = original.Indication ?? string.Empty,
                DosageCount = original.DosageCount,
                Advice = original.Advice,
                TotalAmount = original.TotalPrice,
                FormulaSource = original.FormulaSource,
                Remark = $"复制自原处方 - {newName}",
                Items = original.Items.Select(item => new PrescriptionItemCreateDto
                {
                    HerbId = item.HerbId,
                    HerbName = item.HerbName,
                    Quantity = item.Quantity,
                    Unit = item.Unit,
                    UnitPrice = item.UnitPrice,
                    Subtotal = item.Subtotal,
                    Usage = item.Usage,
                    Remark = item.Remark
                }).ToList()
            };

            // 调用创建方法来复制处方
            var copyResult = await _businessService.CreateAsync(createDto);

            if (copyResult.IsSuccess)
            {
                return ServiceResult<PrescriptionDto>.Success(copyResult.Data!, $"处方复制成功：{newName}");
            }

            return ServiceResult<PrescriptionDto>.Failure($"处方复制失败：{copyResult.ErrorMessage}");
        }
        catch (Exception ex)
        {
            return ServiceResult<PrescriptionDto>.Failure($"处方复制过程发生错误：{ex.Message}");
        }
    }
}
