using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Sync.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers;

/// <summary>
/// 数据同步 API - 基础数据（Herb/Patient/Formula）的双向同步
/// OpenSpec: implement-data-sync
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Policy = "DoctorOrAdmin")]
public class SyncController : BaseApiController
{
    private readonly ISyncService _syncService;

    public SyncController(
        ISyncService syncService,
        ILogger<SyncController> logger)
        : base(logger)
    {
        _syncService = syncService;
    }

    /// <summary>
    /// 获取支持的实体类型列表
    /// </summary>
    [HttpGet("entity-types")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<string>>), 200)]
    public IActionResult GetEntityTypes()
    {
        var types = _syncService.GetSupportedEntityTypes();
        return Success(types, "获取成功");
    }

    /// <summary>
    /// 获取指定实体类型的元数据（用于客户端比对）
    /// </summary>
    /// <param name="entityType">实体类型 (Herb/Patient/Formula)</param>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(ApiResponse<List<SyncMetadataDto>>), 200)]
    public async Task<IActionResult> GetMetadata([FromQuery] string entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return ValidationFail("实体类型不能为空");
        }

        var result = await _syncService.GetMetadataAsync(entityType);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage!);
        }

        return Success(result.Data!, "获取元数据成功");
    }

    /// <summary>
    /// 比对本地与服务器的差异
    /// </summary>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(ApiResponse<SyncCompareResultDto>), 200)]
    public async Task<IActionResult> Compare([FromBody] SyncCompareInputDto input)
    {
        if (ValidateModel() is { } error) return error;

        if (string.IsNullOrWhiteSpace(input.EntityType))
        {
            return ValidationFail("实体类型不能为空");
        }

        var result = await _syncService.CompareAsync(input);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage!);
        }

        return Success(result.Data!, "比对完成");
    }

    /// <summary>
    /// 上传本地数据到服务器
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ApiResponse<SyncUploadResultDto>), 200)]
    public async Task<IActionResult> Upload([FromBody] SyncUploadInputDto input)
    {
        if (ValidateModel() is { } error) return error;

        if (string.IsNullOrWhiteSpace(input.EntityType))
        {
            return ValidationFail("实体类型不能为空");
        }

        if (input.Entities == null || input.Entities.Count == 0)
        {
            return ValidationFail("上传数据不能为空");
        }

        LogOperation("数据同步上传", new { input.EntityType, Count = input.Entities.Count });

        var result = await _syncService.UploadAsync(input);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage!);
        }

        return Success(result.Data!, "上传完成");
    }

    /// <summary>
    /// 从服务器下载数据
    /// </summary>
    [HttpPost("download")]
    [ProducesResponseType(typeof(ApiResponse<SyncDownloadResultDto>), 200)]
    public async Task<IActionResult> Download([FromBody] SyncDownloadInputDto input)
    {
        if (ValidateModel() is { } error) return error;

        if (string.IsNullOrWhiteSpace(input.EntityType))
        {
            return ValidationFail("实体类型不能为空");
        }

        if (input.EntityIds == null || input.EntityIds.Count == 0)
        {
            return ValidationFail("下载实体ID列表不能为空");
        }

        var result = await _syncService.DownloadAsync(input);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage!);
        }

        return Success(result.Data!, "下载完成");
    }

    /// <summary>
    /// 同步删除操作（带引用检查）
    /// </summary>
    [HttpPost("delete")]
    [ProducesResponseType(typeof(ApiResponse<SyncDeleteResultDto>), 200)]
    public async Task<IActionResult> Delete([FromBody] SyncDeleteInputDto input)
    {
        if (ValidateModel() is { } error) return error;

        if (string.IsNullOrWhiteSpace(input.EntityType))
        {
            return ValidationFail("实体类型不能为空");
        }

        if (input.EntityIds == null || input.EntityIds.Count == 0)
        {
            return ValidationFail("删除实体ID列表不能为空");
        }

        LogOperation("数据同步删除", new { input.EntityType, Count = input.EntityIds.Count });

        var result = await _syncService.DeleteAsync(input);
        if (!result.IsSuccess)
        {
            return BusinessFail(result.ErrorMessage!);
        }

        return Success(result.Data!, "删除操作完成");
    }
}
