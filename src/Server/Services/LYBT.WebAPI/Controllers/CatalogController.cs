using Asp.Versioning;
using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Catalog.Application.Commands;
using LYBT.Module.Catalog.Application.Queries;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材方剂目录 API（A-31-C3b 合并 HerbsController + FormulasController）。
    /// 路由保持：/api/v1/herbs/*（药材）+ /api/v1/formulas/*（验方）。
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/herbs")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class CatalogController : BaseCrudController
    {
        private readonly ICatalogQueryService<HerbListDto, HerbDetailDto> _herbService;
        private readonly ICatalogQueryService<FormulaListDto, FormulaDetailDto> _formulaService;

        public CatalogController(
            ISender sender,
            ILogger<CatalogController> logger,
            ICatalogQueryService<HerbListDto, HerbDetailDto> herbService,
            ICatalogQueryService<FormulaListDto, FormulaDetailDto> formulaService
        )
            : base(sender, logger)
        {
            _herbService = herbService;
            _formulaService = formulaService;
        }

        #region 药材端点（原 HerbsController，路由 /api/v1/herbs/*）

        /// <summary>
        /// 获取药材分页列表
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<HerbListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] CommonStatus? status = null,
            CancellationToken ct = default
        )
        {
            if (ValidatePagination(page, pageSize) is { } error)
                return error;

            var result = await _herbService.GetPagedAsync(page, pageSize, keyword, null, false, ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 下载药材导入 JSON 模板（2026-08-13：Excel→JSON——后端不涉及 Excel 格式，保持通用性）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("import-template")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult HerbImportTemplate()
        {
            var template = new
            {
                Description = "药材批量导入 JSON 模板（与 POST /herbs/batch-import 期望的 DTO 一致）",
                Fields = new[]
                {
                    new
                    {
                        Field = "Name",
                        Required = true,
                        Description = "药材名称",
                    },
                    new
                    {
                        Field = "PinYinCode",
                        Required = false,
                        Description = "拼音码",
                    },
                    new
                    {
                        Field = "Category",
                        Required = false,
                        Description = "分类",
                    },
                    new
                    {
                        Field = "Properties",
                        Required = false,
                        Description = "性味",
                    },
                    new
                    {
                        Field = "Origin",
                        Required = false,
                        Description = "产地",
                    },
                    new
                    {
                        Field = "Spec",
                        Required = false,
                        Description = "规格",
                    },
                    new
                    {
                        Field = "Unit",
                        Required = false,
                        Description = "单位（默认 克）",
                    },
                    new
                    {
                        Field = "Price",
                        Required = false,
                        Description = "单价",
                    },
                    new
                    {
                        Field = "CostPrice",
                        Required = false,
                        Description = "成本价",
                    },
                },
                Example = new[]
                {
                    new
                    {
                        Name = "人参",
                        PinYinCode = "renshen",
                        Category = "补益药",
                        Properties = "甘微苦温",
                        Origin = "吉林",
                        Spec = "一等",
                        Unit = "克",
                        Price = 10.5m,
                        CostPrice = 5.0m,
                    },
                },
            };
            return Success(template, "药材导入模板（JSON）");
        }

        /// <summary>
        /// 导出全部药材为 JSON 数组（2026-08-13：Excel→JSON）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("export-all")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbListDto>>), 200)]
        public async Task<IActionResult> HerbExportAll(
            [FromQuery] string? keyword = null,
            CancellationToken ct = default
        )
        {
            var result = await _herbService.GetPagedAsync(1, 10000, keyword, null, false, ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "导出失败");

            // JSON 数组
            return Success(result.Value!.Items, "药材导出（JSON）");
        }

        /// <summary>
        /// 导出药材为 JSON 数组（按筛选条件，US-HERB-013——Desktop 契约 GET /herbs/export，对齐患者模式）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("export")]
        [ProducesResponseType(typeof(ApiResponse<List<HerbListDto>>), 200)]
        public async Task<IActionResult> HerbExport(
            [FromQuery] string? keyword = null,
            CancellationToken ct = default
        )
        {
            var result = await _herbService.GetPagedAsync(1, 10000, keyword, null, false, ct);
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "导出失败");

            // JSON 数组
            return Success(result.Value!.Items, "药材导出（JSON）");
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<HerbDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error)
                return error;

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
        public async Task<IActionResult> Create([FromBody] HerbInputDto input, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new CreateEntityCommand<HerbInputDto, HerbDetailDto>(input, operatorId),
                ct
            );
            if (result.IsSuccess && result.Value != null)
            {
                LogOperation("创建药材", result.Value, null);
                return CreatedAtAction(
                    nameof(GetById),
                    new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                    ApiResponse<HerbDetailDto>.CreateSuccess(result.Value, "药材创建成功")
                );
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
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] HerbInputDto input,
            CancellationToken ct
        )
        {
            if (ValidateGuid(id, "药材ID") is { } error)
                return error;

            // P1-7（2026-08-14）: 存在性+所有权检查移入 CommandHandler——Controller 仅编排
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new UpdateEntityCommand<HerbInputDto, HerbDetailDto>(id, input, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权更新该药材");
                return BusinessFail(result.Error ?? "更新失败");
            }

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
            if (ValidateGuid(id, "药材ID") is { } error)
                return error;

            // P1-7（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new DeleteEntityCommand<Herb>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权删除该药材");
                return BusinessFail(result.Error ?? "删除失败");
            }

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
            // P1-7（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new ToggleEntityStatusCommand<Herb, HerbDetailDto>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权切换该药材状态");
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换药材状态", new { NewStatus = result.Value.Status }, id);
            return Success(
                result.Value,
                $"药材已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}"
            );
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
            if (ValidateGuid(id, "药材ID") is { } error)
                return error;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new RestoreEntityCommand<Herb, HerbDetailDto>(id, operatorId),
                ct
            );
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
        public override async Task<IActionResult> BatchDelete(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct
        ) =>
            await ExecuteBatchDeleteAsync(
                dto,
                (ids, operatorId) => new BatchDeleteHerbsCommand(ids, operatorId),
                "请至少选择一个药材",
                "批量删除药材",
                ct
            );

        /// <summary>
        /// 批量导入药材（JSON）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<HerbBatchImportResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchImport(
            [FromBody] HerbBatchImportInputDto request,
            CancellationToken ct
        )
        {
            if (request?.Herbs == null || request.Herbs.Count == 0)
            {
                return ValidationFail("导入列表不能为空");
            }

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new BatchImportHerbsCommand(request.Herbs, request.Strategy, operatorId),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation(
                "批量导入药材",
                new { Count = request.Herbs.Count, Strategy = request.Strategy },
                null
            );
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条药材");
        }

        /// <summary>
        /// 检查药材引用关系
        /// </summary>
        [HttpGet("{id}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<HerbReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "药材ID") is { } error)
                return error;

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
            CancellationToken ct
        ) =>
            await ExecuteBatchCheckReferenceAsync(
                dto.HerbIds,
                ids => new BatchCheckHerbReferenceQuery(ids),
                "药材ID列表不能为空",
                "单次最多检查100条药材",
                "批量引用检查失败",
                ct
            );

        /// <summary>
        /// 批量启用药材
        /// </summary>
        [HttpPost("batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnable(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await Sender.Send(new BatchEnableHerbsCommand(dto.Ids), ct);
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
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("药材ID列表不能为空");

            var result = await Sender.Send(new BatchDisableHerbsCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药材", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #endregion

        #region 验方端点（原 FormulasController，绝对路由 /api/v1/formulas/*）

        /// <summary>
        /// 获取验方分页列表
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaListDto>>), 200)]
        public async Task<IActionResult> GetFormulaList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default
        )
        {
            if (ValidatePagination(page, pageSize) is { } error)
                return error;

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _formulaService.GetPagedAsync(
                page,
                pageSize,
                keyword,
                operatorId,
                isAdmin,
                ct
            );
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return SuccessPaged(result.Value!, "查询成功");
        }

        /// <summary>
        /// 下载验方导入模板（T4 P0#4: 此前端点缺失桌面调用 404）
        /// </summary>
        /// <summary>
        /// 下载验方导入 JSON 模板（2026-08-13：Excel→JSON——后端不涉及 Excel 格式，保持通用性）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("/api/v{version:apiVersion}/formulas/import-template")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public IActionResult FormulaImportTemplate()
        {
            var template = new
            {
                Description = "验方批量导入 JSON 模板（与 POST /formulas/batch-import 期望的 DTO 一致）",
                Fields = new[]
                {
                    new
                    {
                        Field = "Name",
                        Required = true,
                        Description = "验方名称",
                    },
                    new
                    {
                        Field = "Category",
                        Required = false,
                        Description = "分类",
                    },
                    new
                    {
                        Field = "Effect",
                        Required = false,
                        Description = "功效",
                    },
                    new
                    {
                        Field = "Usage",
                        Required = false,
                        Description = "用法",
                    },
                    new
                    {
                        Field = "Herbs",
                        Required = true,
                        Description = "药材组成（对象数组 [{HerbName, Dosage, Unit}]，如 [{\"HerbName\":\"人参\",\"Dosage\":10,\"Unit\":\"g\"}]）",
                    },
                },
                Example = new[]
                {
                    new
                    {
                        Name = "四君子汤",
                        Category = "补益剂",
                        Effect = "益气健脾",
                        Usage = "水煎服",
                        Herbs = new[]
                        {
                            new { HerbName = "人参", Dosage = 10, Unit = "g" },
                            new { HerbName = "白术", Dosage = 10, Unit = "g" },
                        },
                    },
                },
            };
            return Success(template, "验方导入模板（JSON）");
        }

        /// <summary>
        /// 导出验方为 JSON 数组（含药材组成明细，2026-08-13：Excel→JSON；P2：按分类筛选，对齐客户端 category）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpGet("/api/v{version:apiVersion}/formulas/export")]
        [ProducesResponseType(typeof(ApiResponse<List<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> FormulaExport(
            [FromQuery] string? category = null,
            CancellationToken ct = default
        )
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _formulaService.ExportDetailsAsync(
                keyword: null,
                category: category,
                operatorId: operatorId,
                isAdmin: isAdmin,
                ct: ct
            );
            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "导出失败");

            // JSON 数组（含 Herbs 明细，满足 US-FORM-013）
            return Success(result.Value!, "验方导出（JSON）");
        }

        /// <summary>
        /// 获取验方详情
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas/{id}")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> GetFormulaById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            var result = await _formulaService.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "验方不存在");

            var (operatorId, _, operatorRole) = GetOperator();
            if (
                operatorRole == UserRole.Doctor
                && result.Value.CreatedBy != operatorId
                && !result.Value.IsShared
            )
                return Forbid("无权限查看此验方");

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 新增验方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateFormula(
            [FromBody] FormulaInputDto input,
            CancellationToken ct
        )
        {
            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new CreateEntityCommand<FormulaInputDto, FormulaDetailDto>(input, operatorId),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增验方成功", result.Value, null);
            return CreatedAtAction(
                nameof(GetFormulaById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<FormulaDetailDto>.CreateSuccess(result.Value, "验方创建成功")
            );
        }

        /// <summary>
        /// 更新验方信息
        /// </summary>
        [HttpPut("/api/v{version:apiVersion}/formulas/{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        public async Task<IActionResult> UpdateFormula(
            Guid id,
            [FromBody] FormulaInputDto input,
            CancellationToken ct
        )
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler——Controller 仅编排
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new UpdateEntityCommand<FormulaInputDto, FormulaDetailDto>(id, input, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权更新该验方");
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新验方成功", result.Value, id);
            return Success(result.Value, "验方更新成功");
        }

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        [HttpDelete("/api/v{version:apiVersion}/formulas/{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public async Task<IActionResult> DeleteFormula(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new DeleteEntityCommand<Formula>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权删除该验方");
                return NotFound(result.Error ?? "验方不存在");
            }

            LogOperation("删除验方成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 切换验方启用/禁用状态
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/{id}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> ToggleFormulaStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            // P1-8（2026-08-14）: 存在性+所有权检查移入 CommandHandler
            var (operatorId, _, operatorRole) = GetOperator();
            var result = await Sender.Send(
                new ToggleEntityStatusCommand<Formula, FormulaDetailDto>(id, operatorId, operatorRole),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.ErrorCode == ErrorCode.Forbidden)
                    return Forbid(result.Error ?? "无权切换该验方状态");
                return BusinessFail(result.Error ?? "切换状态失败");
            }

            LogOperation("切换验方状态", new { NewStatus = result.Value.Status }, id);
            return Success(
                result.Value,
                $"验方已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}"
            );
        }

        /// <summary>
        /// 恢复已删除的验方 — 仅 Admin（业务管理）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
        [HttpPost("/api/v{version:apiVersion}/formulas/{id}/restore")]
        [ProducesResponseType(typeof(ApiResponse<FormulaDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public async Task<IActionResult> RestoreFormula(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "验方ID") is { } error)
                return error;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(
                new RestoreEntityCommand<Formula, FormulaDetailDto>(id, operatorId),
                ct
            );
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("未被删除") == true)
                    return BusinessFail(result.Error);
                return NotFound(result.Error ?? "验方不存在");
            }

            LogOperation("恢复验方", result.Value, result.Value.Id);
            return Success(result.Value, "验方恢复成功");
        }

        /// <summary>
        /// 批量删除验方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public async Task<IActionResult> BatchDeleteFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct
        ) =>
            await ExecuteBatchDeleteAsync(
                dto,
                (ids, operatorId) => new BatchDeleteFormulasCommand(ids, operatorId),
                "请至少选择一个验方",
                "批量删除验方",
                ct
            );

        /// <summary>
        /// 批量导入验方（JSON）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-import")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<FormulaBatchImportResultDto>), 200)]
        public async Task<IActionResult> ImportFormulas(
            [FromBody] FormulaBatchImportInputDto request,
            CancellationToken ct
        )
        {
            if (request == null || request.Formulas == null || !request.Formulas.Any())
            {
                return ValidationFail("导入数据不能为空");
            }

            var result = await Sender.Send(
                new BatchImportFormulasCommand(request.Formulas, request.FileName),
                ct
            );

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "导入失败");
            }

            LogOperation(
                "批量导入验方",
                new
                {
                    FileName = request.FileName,
                    TotalCount = result.Value.TotalCount,
                    SuccessCount = result.Value.SuccessCount,
                },
                null
            );

            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 获取待校验验方列表
        /// </summary>
        [HttpGet("/api/v{version:apiVersion}/formulas/pending-validation")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<FormulaDetailDto>>), 200)]
        public async Task<IActionResult> GetPendingValidation(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default
        )
        {
            var result = await Sender.Send(new GetPendingValidationQuery(page, pageSize), ct);

            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "查询失败");
            }

            return SuccessPaged(result.Value, $"查询成功，共{result.Value.TotalCount}个待校验验方");
        }

        /// <summary>
        /// 校验验方药材匹配
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/{formulaId}/herbs/{herbItemId}/validate")]
        [ProducesResponseType(typeof(ApiResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ValidateHerb(
            Guid formulaId,
            Guid herbItemId,
            [FromBody] ValidateFormulaHerbInputDto request,
            CancellationToken ct
        )
        {
            if (ValidateGuid(formulaId, "验方ID") is { } error1)
                return error1;
            if (ValidateGuid(herbItemId, "药材项ID") is { } error2)
                return error2;
            if (ValidateGuid(request.SelectedHerbId, "系统药材ID") is { } error3)
                return error3;

            var result = await Sender.Send(
                new ValidateFormulaHerbCommand(formulaId, herbItemId, request.SelectedHerbId),
                ct
            );

            if (!result.IsSuccess)
            {
                return BusinessFail(result.Error ?? "验证失败");
            }

            LogOperation(
                "验证验方药材",
                new
                {
                    FormulaId = formulaId,
                    HerbItemId = herbItemId,
                    SelectedHerbId = request.SelectedHerbId,
                },
                formulaId
            );

            return Success("药材验证成功");
        }

        /// <summary>
        /// 批量启用药方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-enable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchEnableFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct = default
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");

            var result = await Sender.Send(new BatchEnableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量启用失败");

            LogOperation("批量启用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 批量禁用药方
        /// </summary>
        [HttpPost("/api/v{version:apiVersion}/formulas/batch-disable")]
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        public async Task<IActionResult> BatchDisableFormulas(
            [FromBody] BatchDeleteInputDto dto,
            CancellationToken ct = default
        )
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
                return ValidationFail("验方ID列表不能为空");

            var result = await Sender.Send(new BatchDisableFormulasCommand(dto.Ids), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "批量禁用失败");

            LogOperation("批量禁用药方", new { Count = dto.Ids.Count }, null);
            return Success(result.Value, result.Value.Message);
        }

        #endregion
    }
}
