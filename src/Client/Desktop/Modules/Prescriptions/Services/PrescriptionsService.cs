using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Prescriptions.Services;

/// <summary>
/// 处方管理服务 - 简化版，只包含基础CRUD
/// </summary>
public class PrescriptionsService : IPrescriptionService
{
    public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
    {
        // 简化实现：返回空列表
        var emptyResult = new PagedResult<PrescriptionDto>
        {
            Items = new List<PrescriptionDto>(),
            TotalCount = 0,
            CurrentPage = page,
            PageSize = pageSize
        };
        return ServiceResult<PagedResult<PrescriptionDto>>.Success(emptyResult);
    }

    public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
    {
        return ServiceResult<PrescriptionDto>.Failure("处方不存在");
    }

    public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
    {
        return ServiceResult<PrescriptionDto>.Failure("创建功能暂未实现");
    }

    public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionUpdateDto dto)
    {
        return ServiceResult<PrescriptionDto>.Failure("更新功能暂未实现");
    }

    public async Task<ServiceResult> DeleteAsync(Guid id)
    {
        return ServiceResult.Failure("删除功能暂未实现");
    }
}