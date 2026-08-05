using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Herbs.Application.Commands;
using LYBT.Module.Herbs.Application.Queries;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using LYBT.WebAPI.Services;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class HerbsController : BaseCrudController
    {
        private readonly IHerbService _herbService;
        private readonly ExcelService _excelService;

        public HerbsController(ISender sender, ILogger<HerbsController> logger, IHerbService herbService, ExcelService excelService)
            : base(sender, logger)
        {
            _herbService = herbService;
            _excelService = excelService;
        }

        /// <summary>
        /// 获取药材分页列表
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "HerbsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var result = await _herbService.GetPagedAsync(page, pageSize, keyword, ct);
            if (!result.IsSuccess) return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await _herbService.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "药材不存在");

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), StatusCodes.Status201Created)]
        public override async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
        {
            if (dto is not HerbInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new CreateHerbCommand(inputDto, operatorId), ct);
            if (result.IsSuccess && result.Value != null)
            {
                LogOperation("创建药材", result.Value, null);
                return CreatedAtAction(nameof(GetById),
                    new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Value, "药材创建成功"));
            }

            return BusinessFail(result.Error ?? "创建失败");
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;
            if (dto is not HerbInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var getResult = await _herbService.GetByIdAsync(id, ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;

            var (operatorId, _, _) = GetOperator();
            var result = await _herbService.UpdateAsync(id, inputDto, operatorId, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "更新失败");

            LogOperation("更新药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材更新成功");
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var getResult = await _herbService.GetByIdAsync(id, ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new DeleteHerbCommand(id, operatorId), ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "删除失败");

            LogOperation("删除药材", new { Id = id }, id);
            return Success<object?>(null, "药材删除成功");
        }

        /// <summary>
        /// 切换药材状态
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();

            var getResult = await _herbService.GetByIdAsync(id, ct);
            if (!getResult.IsSuccess || getResult.Value == null)
                return NotFound(getResult.Error ?? "药材不存在");

            if (ValidateOwnership(getResult.Value.CreatedBy, "药材") is { } ownerError)
                return ownerError;

            var result = await _herbService.ToggleStatusAsync(id, operatorId, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "切换状态失败");

            LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的药材
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var (operatorId, _, _) = GetOperator();
            var result = await _herbService.RestoreAsync(id, operatorId, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "恢复失败");

            LogOperation("恢复药材", result.Value, result.Value.Id);
            return Success(result.Value, "药材恢复成功");
        }

        /// <summary>
        /// 批量删除药材
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
            => await ExecuteBatchDeleteAsync(
                dto,
                (ids, operatorId) => new BatchDeleteHerbsCommand(ids, operatorId),
                "请至少选择一个药材",
                "批量删除药材",
                ct);

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport([FromBody] HerbBatchImportInputDto request, CancellationToken ct)
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("导入列表不能为空");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation("批量导入药材", new { Count = request.Herbs.Count, Strategy = request.Strategy }, null);
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
        }

        /// <summary>
        /// 检查药材引用关系
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error) return error;

            var result = await Sender.Send(new CheckHerbReferenceQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "药材不存在");
            return Success(result.Value, "引用检查完成");
        }

        /// <summary>
        /// 批量检查药材引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbReferenceCheckDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchCheckReference(
            [FromBody] HerbBatchCheckReferenceInputDto dto,
            CancellationToken ct)
            => await ExecuteBatchCheckReferenceAsync(
                dto.HerbIds,
                ids => new BatchCheckHerbReferenceQuery(ids),
                "药材ID列表不能为空",
                "单次最多检查100条药材",
                "批量引用检查失败",
                ct);

        /// <summary>
        /// 批量启用药材
        /// </summary>
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnable(
            [FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await _herbService.BatchEnableAsync(dto.Ids, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");

            LogOperation("批量启用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 批量禁用药材
        /// </summary>
        [HttpPost("batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisable(
            [FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await _herbService.BatchDisableAsync(dto.Ids, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 导出药材数据到 Excel（keyword 可选）
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> Export([FromQuery] string? keyword, CancellationToken ct)
        {
            var herbs = await GetAllDetailsAsync(keyword, ct);
            if (herbs == null) return BusinessFail("导出失败");

            var bytes = _excelService.ExportToExcel(herbs, "药材", new Dictionary<string, Func<HerbDetailDto, object?>>
            {
                ["名称"] = h => h.Name,
                ["拼音码"] = h => h.PinYinCode,
                ["分类"] = h => h.Category,
                ["性味"] = h => h.Properties,
                ["功效"] = h => h.Effect,
                ["用法用量"] = h => h.Usage,
                ["单位"] = h => h.Unit,
                ["单价"] = h => h.Price,
                ["产地"] = h => h.Origin,
                ["备注"] = h => h.Remark
            });

            return File(bytes, ExcelService.ExcelContentType, $"药材导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        /// <summary>
        /// 下载药材导入模板（表头 + 示例行）
        /// </summary>
        [HttpGet("import-template")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public IActionResult ImportTemplate()
        {
            var bytes = _excelService.GenerateTemplate("药材", new Dictionary<string, string>
            {
                ["名称"] = "当归",
                ["拼音码"] = "danggui",
                ["分类"] = "补血药",
                ["性味"] = "甘、辛、温",
                ["功效"] = "补血活血，调经止痛",
                ["用法用量"] = "6-12g，煎服",
                ["单位"] = "克",
                ["单价"] = "0.05",
                ["产地"] = "甘肃",
                ["备注"] = ""
            });

            return File(bytes, ExcelService.ExcelContentType, "药材导入模板.xlsx");
        }

        /// <summary>
        /// 从 Excel 文件批量导入药材（仅 Admin+），复用 BatchImportHerbsCommand（拼音自动生成 + 重复策略）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("batch-import-excel")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        public async Task<IActionResult> BatchImportExcel(
            IFormFile file,
            [FromForm] DuplicateStrategy strategy = DuplicateStrategy.Skip,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return ValidationFail("请上传 Excel 文件");

            List<HerbInputDto> herbs;
            try
            {
                using var stream = file.OpenReadStream();
                herbs = _excelService.ParseExcel(stream, new Dictionary<string, Action<HerbInputDto, string>>
                {
                    ["名称"] = (d, v) => d.Name = v,
                    ["拼音码"] = (d, v) => d.PinYinCode = v,
                    ["分类"] = (d, v) => d.Category = v,
                    ["性味"] = (d, v) => d.Properties = v,
                    ["功效"] = (d, v) => d.Effect = v,
                    ["用法用量"] = (d, v) => d.Usage = v,
                    ["单位"] = (d, v) => d.Unit = string.IsNullOrWhiteSpace(v) ? "克" : v,
                    ["单价"] = (d, v) => d.Price = decimal.TryParse(v, out var p) ? p : 0,
                    ["产地"] = (d, v) => d.Origin = v,
                    ["备注"] = (d, v) => d.Remark = v
                });
            }
            catch
            {
                return ValidationFail("Excel 文件解析失败，请使用系统模板");
            }

            if (herbs.Count == 0)
                return ValidationFail("导入列表不能为空");

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchImportHerbsCommand(herbs, strategy, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "导入失败");

            LogOperation("批量导入药材(Excel)", new { Count = herbs.Count, Strategy = strategy }, null);
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
        }

        /// <summary>
        /// 全量拉取药材详情（分页循环 + 逐条详情）。失败返回 null。
        /// </summary>
        private async Task<List<HerbDetailDto>?> GetAllDetailsAsync(string? keyword, CancellationToken ct)
        {
            var details = new List<HerbDetailDto>();
            const int pageSize = 100;
            var page = 1;

            while (true)
            {
                var paged = await _herbService.GetPagedAsync(page, pageSize, keyword, ct);
                if (!paged.IsSuccess || paged.Value == null) return null;
                if (paged.Value.Items.Count == 0) break;

                foreach (var item in paged.Value.Items)
                {
                    var detail = await _herbService.GetByIdAsync(item.Id, ct);
                    if (detail.IsSuccess && detail.Value != null) details.Add(detail.Value);
                }

                if (details.Count >= paged.Value.TotalCount || paged.Value.Items.Count < pageSize) break;
                page++;
            }

            return details;
        }
    }
}
