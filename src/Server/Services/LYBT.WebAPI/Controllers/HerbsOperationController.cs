using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{

    /// <summary>
    /// 药材业务操作 API 控制器 - 处理导入、批量更新、状态管理等业务操作
    /// 对应 IHerbBusinessService 的业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/herbs/operation")]
    [Authorize]
    public class HerbsOperationController : BaseApiController
    {
        private readonly IHerbBusinessService _businessService;

        /// <summary>
        /// 构造方法，注入药材业务服务
        /// </summary>
        public HerbsOperationController(
            IHerbBusinessService businessService,
            IMemoryCache memoryCache,
            ILogger<HerbsOperationController> logger)
            : base(logger, memoryCache)
        {
            _businessService = businessService;
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult<ApiResponse<int>>> Import([FromBody] List<HerbImportDto> dtos)
        {
            try
            {
                if (dtos == null || dtos.Count == 0)
                {
                    return ValidationFail<int>("导入数据不能为空", "INVALID_IMPORT_DATA");
                }

                if (dtos.Count > 1000)
                {
                    return ValidationFail<int>("单次导入不能超过1000条记录", "IMPORT_LIMIT_EXCEEDED");
                }

                var result = await _businessService.ImportHerbsAsync(dtos);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<int>(result.ErrorMessage ?? "批量导入失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("批量导入药材", new { Count = result.Data, TotalSubmitted = dtos.Count }, null);
                return Success(result.Data, $"成功导入 {result.Data} 个药材");
            }
            catch (Exception ex)
            {
                return HandleException<int>(ex, "批量导入药材", dtos?.Count);
            }
        }

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        [HttpPost("batch-update-status")]
        public async Task<ActionResult<ApiResponse<bool>>> BatchUpdateStatus([FromBody] BatchUpdateStatusDto dto)
        {
            try
            {
                if (dto == null || dto.Ids == null || dto.Ids.Count == 0)
                {
                    return ValidationFail<bool>("药材ID列表不能为空", "INVALID_IDS");
                }

                var result = await _businessService.BatchUpdateStatusAsync(dto.Ids, dto.Status, dto.Reason);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "批量更新状态失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("批量更新药材状态", new { Count = dto.Ids.Count, Status = dto.Status }, null);
                return Success(result.Data, $"成功更新 {dto.Ids.Count} 个药材的状态");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "批量更新药材状态", dto);
            }
        }

        /// <summary>
        /// 软删除药材
        /// </summary>
        [HttpDelete("{id:guid}/soft")]
        public async Task<ActionResult<ApiResponse<bool>>> SoftDelete(Guid id)
        {
            try
            {
                var result = await _businessService.SoftDeleteAsync(id);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "软删除失败", ApiErrorCodes.DATADELETEFAILED);
                }

                LogOperation("软删除药材", null, id);
                return Success(result.Data, "药材已软删除");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "软删除药材", id);
            }
        }

        /// <summary>
        /// 创建药材（自动生成编码）
        /// </summary>
        [HttpPost("create-with-auto-code")]
        public async Task<ActionResult<ApiResponse<HerbDto>>> CreateWithAutoCode([FromBody] HerbCreateDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<HerbDto>("创建数据不能为空", "INVALID_DATA");
                }

                var validationResult = ValidateModel<HerbDto>();
                if (validationResult != null) return validationResult;

                var result = await _businessService.CreateHerbWithAutoCodeAsync(dto);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<HerbDto>(result.ErrorMessage ?? "创建药材失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("创建药材（自动编码）", dto, result.Data.Id);
                return Success(result.Data, "药材创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "创建药材", dto);
            }
        }

        /// <summary>
        /// 设置药材状态
        /// </summary>
        [HttpPost("{id:guid}/set-status")]
        public async Task<ActionResult<ApiResponse<bool>>> SetStatus(Guid id, [FromBody] SetStatusDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<bool>("状态数据不能为空", "INVALID_STATUS");
                }

                var result = await _businessService.SetStatusAsync(id, dto.IsActive);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "设置状态失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("设置药材状态", new { Id = id, IsActive = dto.IsActive }, id);
                return Success(result.Data, dto.IsActive ? "药材已启用" : "药材已禁用");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "设置药材状态", new { id, dto });
            }
        }

    }

    #region DTOs

    /// <summary>
    /// 批量更新状态DTO
    /// </summary>
    public class BatchUpdateStatusDto
    {
        /// <summary>
        /// 药材ID列表
        /// </summary>
        public List<Guid> Ids { get; set; } = new();

        /// <summary>
        /// 状态
        /// </summary>
        public bool Status { get; set; }

        /// <summary>
        /// 原因
        /// </summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 设置状态DTO
    /// </summary>
    public class SetStatusDto
    {
        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsActive { get; set; }
    }

    #endregion DTOs
}
