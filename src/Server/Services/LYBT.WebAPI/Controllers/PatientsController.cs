using Asp.Versioning;
using MediatR;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.Patients.Application.Commands;
using LYBT.Module.Patients.Application.Queries;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using LYBT.WebAPI.Services;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 患者管理 API
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
    public class PatientsController : BaseCrudController
    {
        private readonly IPatientService _patientService;
        private readonly ExcelService _excelService;

        public PatientsController(ISender sender, ILogger<PatientsController> logger, IPatientService patientService, ExcelService excelService)
            : base(sender, logger)
        {
            _patientService = patientService;
            _excelService = excelService;
        }

        /// <summary>
        /// 获取患者列表 - 支持分页和查询
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "PatientsCache")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientListDto>>), 200)]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;

            var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;

            var result = await _patientService.GetPagedAsync(page, pageSize, keyword, filterDisabled: !isAdmin, ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "查询失败");

            return SuccessPaged(result.Value, "查询成功");
        }

        /// <summary>
        /// 获取患者详情
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } error) return error;

            var result = await _patientService.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "患者不存在");

            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 新增患者
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), StatusCodes.Status201Created)]
        public override async Task<IActionResult> Create([FromBody] object dto, CancellationToken ct)
        {
            if (dto is not PatientInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new CreatePatientCommand(inputDto, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "创建失败");
            }

            LogOperation("新增患者成功", result.Value, null);
            return CreatedAtAction(nameof(GetById),
                new { id = result.Value.Id, version = ApiVersionConstants.V1 },
                ApiResponse<PatientDetailDto>.CreateSuccess(result.Value, "患者创建成功"));
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        [HttpPut("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> Update(Guid id, [FromBody] object dto, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;
            if (dto is not PatientInputDto inputDto)
                return ValidationFail("无效的请求数据");

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await _patientService.UpdateAsync(id, inputDto, operatorId, ct);
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("不存在") == true)
                    return NotFound(result.Error);
                return BusinessFail(result.Error ?? "更新失败");
            }

            LogOperation("更新患者成功", result.Value, id);
            return Success(result.Value, "患者更新成功");
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpDelete("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new DeletePatientCommand(id, operatorId), ct);
            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("医案记录") == true)
                    return BusinessFail(result.Error);
                return NotFound("患者不存在");
            }

            LogOperation("删除患者成功", null, id);
            return Success(true, "删除成功");
        }

        /// <summary>
        /// 切换患者状态（启用/禁用）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("{id:guid}/toggle-status")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        public override async Task<IActionResult> ToggleStatus(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (ownerDto, ownershipError) = await CheckOwnershipAsync(id, ct);
            if (ownershipError != null) return ownershipError;

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new TogglePatientStatusCommand(id, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "操作失败");
            }

            LogOperation("切换患者状态", new { NewStatus = result.Value.Status }, id);
            return Success(result.Value, $"患者已{(result.Value.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
        }

        /// <summary>
        /// 恢复已删除的患者 — 仅 Admin（业务管理）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminBusinessOnly)]
        [HttpPost("{id:guid}/restore")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public override async Task<IActionResult> Restore(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var (operatorId, _, _) = GetOperator();
            var result = await _patientService.RestoreAsync(id, operatorId, ct);
            if (!result.IsSuccess || result.Value == null)
            {
                if (result.Error?.Contains("未被删除") == true)
                    return BusinessFail(result.Error);
                return NotFound(result.Error ?? "患者不存在");
            }

            LogOperation("恢复患者成功", result.Value, id);
            return Success(result.Value, "患者恢复成功");
        }

        /// <summary>
        /// 批量删除患者
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
            => await ExecuteBatchDeleteAsync(
                dto,
                (ids, operatorId) => new BatchDeletePatientsCommand(ids, operatorId),
                "请至少选择一个患者",
                "批量删除患者",
                ct);

        /// <summary>
        /// 检查患者是否被医案引用
        /// </summary>
        [HttpGet("{id:guid}/check-reference")]
        [ProducesResponseType(typeof(ApiResponse<PatientReferenceCheckDto>), 200)]
        public async Task<IActionResult> CheckReference(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "患者ID") is { } guidError) return guidError;

            var result = await Sender.Send(new CheckPatientReferenceQuery(id), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return NotFound(result.Error ?? "患者不存在");
            }

            return Success(result.Value, "引用检查完成");
        }

        /// <summary>
        /// 根据身份证号查询患者
        /// </summary>
        [HttpGet("by-id-number/{idNumber}")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        public async Task<IActionResult> GetByIdNumber(string idNumber, CancellationToken ct)
        {
            var result = await _patientService.GetByIdNumberAsync(idNumber, ct);
            if (!result.IsSuccess || result.Value == null)
                return NotFound(result.Error ?? "未找到匹配的患者");
            return Success(result.Value, "查询成功");
        }

        /// <summary>
        /// 批量检查多个患者的引用关系
        /// </summary>
        [HttpPost("batch-check-reference")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientReferenceCheckDto>>), 200)]
        public async Task<IActionResult> BatchCheckReference([FromBody] PatientBatchCheckReferenceInputDto dto, CancellationToken ct)
            => await ExecuteBatchCheckReferenceAsync(
                dto.PatientIds,
                ids => new BatchCheckPatientReferenceQuery(ids),
                "请至少选择一个患者",
                "批量检查最多支持100条",
                "批量检查失败",
                ct);

        /// <summary>
        /// 通过ISender查询患者并验证所有权
        /// </summary>
        private async Task<(PatientDetailDto? dto, IActionResult? error)> CheckOwnershipAsync(Guid id, CancellationToken ct)
        {
            var result = await _patientService.GetByIdAsync(id, ct);
            if (!result.IsSuccess || result.Value == null)
                return (null, NotFound("患者不存在"));

            if (ValidateOwnership(result.Value.CreatedBy, "患者") is { } ownerError)
                return (null, ownerError);

            return (result.Value, null);
        }

        /// <summary>
        /// 导出患者数据到 Excel（keyword 可选；非 Admin 仅导出启用状态）
        /// </summary>
        [HttpGet("export")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public async Task<IActionResult> Export([FromQuery] string? keyword, CancellationToken ct)
        {
            var patients = await GetAllDetailsAsync(keyword, ct);
            if (patients == null) return BusinessFail("导出失败");

            var bytes = _excelService.ExportToExcel(patients, "患者", new Dictionary<string, Func<PatientDetailDto, object?>>
            {
                ["姓名"] = p => p.Name,
                ["性别"] = p => p.Gender,
                ["出生日期"] = p => p.BirthDate,
                ["身份证号"] = p => p.IdNumber,
                ["电话"] = p => p.PhoneNumber,
                ["拼音码"] = p => p.PinYinCode,
                ["状态"] = p => p.Status
            });

            return File(bytes, ExcelService.ExcelContentType, $"患者导出_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
        }

        /// <summary>
        /// 下载患者导入模板（表头 + 示例行）
        /// </summary>
        [HttpGet("import-template")]
        [ProducesResponseType(typeof(FileResult), 200)]
        public IActionResult ImportTemplate()
        {
            var bytes = _excelService.GenerateTemplate("患者", new Dictionary<string, string>
            {
                ["姓名"] = "张三",
                ["性别"] = "男",
                ["出生日期"] = "1990-01-01",
                ["身份证号"] = "110101199001010011",
                ["电话"] = "13800138000",
                ["拼音码"] = "zhangsan"
            });

            return File(bytes, ExcelService.ExcelContentType, "患者导入模板.xlsx");
        }

        /// <summary>
        /// 从 Excel 文件批量导入患者（仅 Admin+）
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPost("batch-import-excel")]
        [Consumes("multipart/form-data")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<PatientBatchImportResultDto>), 200)]
        public async Task<IActionResult> BatchImportExcel(
            IFormFile file,
            [FromForm] DuplicateStrategy strategy = DuplicateStrategy.Skip,
            CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                return ValidationFail("请上传 Excel 文件");

            List<PatientInputDto> patients;
            try
            {
                using var stream = file.OpenReadStream();
                patients = _excelService.ParseExcel(stream, new Dictionary<string, Action<PatientInputDto, string>>
                {
                    ["姓名"] = (d, v) => d.Name = v,
                    ["性别"] = (d, v) => d.Gender = ParseGender(v),
                    ["出生日期"] = (d, v) => d.BirthDate = DateTime.TryParse(v, out var date) ? date : null,
                    ["身份证号"] = (d, v) => d.IdNumber = v,
                    ["电话"] = (d, v) => d.PhoneNumber = v,
                    ["拼音码"] = (d, v) => d.PinYinCode = v
                });
            }
            catch
            {
                return ValidationFail("Excel 文件解析失败，请使用系统模板");
            }

            if (patients.Count == 0)
                return ValidationFail("导入列表不能为空");

            var (operatorId, _, _) = GetOperator();
            var result = await Sender.Send(new BatchImportPatientsCommand(patients, strategy, operatorId), ct);
            if (!result.IsSuccess || result.Value == null)
                return BusinessFail(result.Error ?? "导入失败");

            LogOperation("批量导入患者(Excel)", new { Count = patients.Count, Strategy = strategy }, null);
            return Success(result.Value, $"成功导入 {result.Value.SuccessCount} 条患者");
        }

        /// <summary>
        /// 全量拉取患者详情（分页循环 + 逐条详情），非 Admin 仅取启用状态。失败返回 null。
        /// </summary>
        private async Task<List<PatientDetailDto>?> GetAllDetailsAsync(string? keyword, CancellationToken ct)
        {
            var isAdmin = User?.IsInRole(RoleConstants.Admin) == true || User?.IsInRole(RoleConstants.SuperAdmin) == true;
            var details = new List<PatientDetailDto>();
            const int pageSize = 100;
            var page = 1;

            while (true)
            {
                var paged = await _patientService.GetPagedAsync(page, pageSize, keyword, filterDisabled: !isAdmin, ct);
                if (!paged.IsSuccess || paged.Value == null) return null;
                if (paged.Value.Items.Count == 0) break;

                foreach (var item in paged.Value.Items)
                {
                    var detail = await _patientService.GetByIdAsync(item.Id, ct);
                    if (detail.IsSuccess && detail.Value != null) details.Add(detail.Value);
                }

                if (details.Count >= paged.Value.TotalCount || paged.Value.Items.Count < pageSize) break;
                page++;
            }

            return details;
        }

        /// <summary>
        /// 解析性别列（男/女/未知 或枚举名）
        /// </summary>
        private static Gender ParseGender(string value)
        {
            return value switch
            {
                "男" or "Male" => Gender.Male,
                "女" or "Female" => Gender.Female,
                _ => Gender.Unknown
            };
        }
    }
}
