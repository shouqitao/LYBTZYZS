using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理服务 - 简化版，只包含基础CRUD
/// </summary>
public class PrescriptionsService : IPrescriptionService
{
    public Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        // 简化实现：返回空列表
        var emptyResult = new PagedResult<PrescriptionDto>
        {
            Items = new List<PrescriptionDto>(),
            TotalCount = 0,
            CurrentPage = page,
            PageSize = pageSize
        };
        return Task.FromResult(ServiceResult<PagedResult<PrescriptionDto>>.Success(emptyResult));
    }

    public Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id) =>
        Task.FromResult(ServiceResult<PrescriptionDto>.Failure("处方不存在"));

    public Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto) =>
        Task.FromResult(ServiceResult<PrescriptionDto>.Failure("创建功能暂未实现"));

    public Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto) =>
        Task.FromResult(ServiceResult<PrescriptionDto>.Failure("更新功能暂未实现"));

    public Task<ServiceResult> DeleteAsync(Guid id) =>
        Task.FromResult(ServiceResult.Failure("删除功能暂未实现"));

    public Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId) =>
        Task.FromResult(ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>()));
}