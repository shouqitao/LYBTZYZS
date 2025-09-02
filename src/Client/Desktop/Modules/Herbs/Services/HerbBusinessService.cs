using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材业务服务实现 - UltraThink双层架构简化版
/// 职责：基础业务操作
/// </summary>
public class HerbBusinessService(ILogger<HerbBusinessService> logger) : IHerbBusinessService
{
    private readonly ILogger<HerbBusinessService> _logger = logger;

    #region 基础业务操作 - 简化实现

    /// <summary>
    /// 创建药材
    /// </summary>
    public async Task<ServiceResult<HerbDto>> CreateAsync(HerbCreateDto createDto)
    {
        return ServiceResult<HerbDto>.Failure("简单诊所版本暂不支持创建药材");
    }

    /// <summary>
    /// 更新药材
    /// </summary>
    public async Task<ServiceResult<HerbDto>> UpdateAsync(Guid id, HerbUpdateDto updateDto)
    {
        return ServiceResult<HerbDto>.Failure("简单诊所版本暂不支持更新药材信息");
    }

    /// <summary>
    /// 启用药材
    /// </summary>
    public async Task<ServiceResult<bool>> EnableAsync(Guid herbId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 禁用药材
    /// </summary>
    public async Task<ServiceResult<bool>> DisableAsync(Guid herbId)
    {
        return ServiceResult<bool>.Success(false);
    }

    /// <summary>
    /// 删除药材
    /// </summary>
    public async Task<ServiceResult<bool>> DeleteAsync(Guid herbId)
    {
        return ServiceResult<bool>.Success(false);
    }

    #endregion
}