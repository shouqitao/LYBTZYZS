using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Module.MedicalCases.Controllers;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Shared.Configuration.Options.Common;
using LYBT.Module.MedicalCases.Mappers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 继承 BaseMedicalCasesController 提供标准方法
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Tags("MedicalCases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCasesController : BaseMedicalCasesController
    {
        private readonly MedicalCaseMapper _medicalCaseMapper;

        public MedicalCasesController(
            ISender sender,
            ILogger<MedicalCasesController> logger,
            IMedicalCaseCommandService medicalCaseCommandService,
            IMedicalCaseQueryService medicalCaseQueryService,
            IMedicalCaseStateService medicalCaseStateService,
            MedicalCaseMapper medicalCaseMapper)
            : base(sender, logger, medicalCaseCommandService, medicalCaseQueryService, medicalCaseStateService)
        {
            _medicalCaseMapper = medicalCaseMapper ?? throw new ArgumentNullException(nameof(medicalCaseMapper));
        }

        /// <summary>
        /// 查询医案列表（分页）
        /// </summary>
        [HttpGet]
        public override async Task<IActionResult> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] CommonStatus? status = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin;
            var result = await _medicalCaseQueryService.GetListDtoAsync(
                status: null,
                patientId: null,
                page: page,
                pageSize: pageSize,
                currentDoctorId: operatorId,
                isAdmin: isAdmin,
                keyword: keyword,
                cancellationToken: ct);

            return Success(result, "查询成功");
        }

        /// <summary>
        /// 获取医案详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        public override async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        {
            if (ValidateGuid(id, "医案ID") is { } error) return error;

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _medicalCaseQueryService.GetDetailDtoAsync(id, operatorId, isAdmin, ct);
            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 批量获取详情（B1 US-MC-018）
        /// </summary>
        [HttpPost("batch-details")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicalCaseDetailDto>>), 200)]
        public async Task<IActionResult> GetBatchDetails([FromBody] BatchIdsRequest request, CancellationToken ct)
        {
            if (request?.Ids == null || request.Ids.Count == 0)
                return ValidationFail("医案ID列表不能为空");
            if (request.Ids.Count > BatchOptions.DefaultMaxBatchSize)
                return ValidationFail($"单次批量查询不能超过 {BatchOptions.DefaultMaxBatchSize} 条");

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _medicalCaseQueryService.GetDetailDtosBatchAsync(request.Ids, operatorId, isAdmin, ct);
            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 创建新医案
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [Authorize(Policy = PolicyConstants.DoctorOnly)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> Create([FromBody] MedicalCaseInputDto input, CancellationToken ct)
        {
            var (doctorId, _, _) = GetOperator();

            input.Id = null;
            var result = await _medicalCaseCommandService.SaveWithDetailAsync(input, doctorId, isAdmin: false, ct);

            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            var responseDto = result.Value!;

            LogOperation("创建医案", input, responseDto.Id);

            return CreatedAtAction(nameof(GetById),
                new { id = responseDto.Id, version = ApiVersionConstants.V1 },
                ApiResponse<MedicalCaseDetailDto>.CreateSuccess(responseDto, "医案创建成功"));
        }

        /// <summary>
        /// 保存医案聚合根
        /// </summary>
        [HttpPut("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> Update(
            Guid id,
            [FromBody] MedicalCaseInputDto input, CancellationToken ct)
        {
            if (input.Id != id)
            {
                return Error("请求ID与路由ID不一致");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _medicalCaseCommandService.SaveWithDetailAsync(input, operatorId, isAdmin, ct);

            if (!result.IsSuccess)
            {
                return HandleResult(result, useAuthMapping: true);
            }

            LogOperation("更新医案", input, id);
            return Success(result.Value!, "保存成功");
        }

        /// <summary>
        /// 删除医案（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // 直接调用 CommandService 删除医案
            var deleted = await _medicalCaseCommandService.DeleteAsync(id, operatorId, isAdmin, ct);
            if (!deleted)
                return HandleResult(Result<bool>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在"), useAuthMapping: true);

            LogOperation("删除医案", null, id);
            return Success(true, "医案已删除");
        }

        /// <summary>
        /// 批量删除医案
        /// </summary>
        [HttpPost("batch-delete")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<BatchOperationResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 400)]
        public override async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto dto, CancellationToken ct)
        {
            if (dto.Ids == null || dto.Ids.Count == 0)
            {
                return ValidationFail("请至少选择一个医案");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _medicalCaseCommandService.BatchDeleteAsync(dto.Ids, operatorId, isAdmin, ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 标记是否需要开处方
        /// </summary>
        [HttpPut("{id}/prescription-flag")]
        [EnableRateLimiting("ApiCalls")]
        public override async Task<IActionResult> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _medicalCaseCommandService.SetPrescriptionFlagWithDetailAsync(
                id, request.NeedsPrescription, operatorId, isAdmin, ct);

            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            LogOperation("更新处方标记", request, id);
            return Success(result.Value!, "处方标记更新成功");
        }

        /// <summary>
        /// 记录打印完成 — 仅 Doctor（打印仅 Doctor，2026-08-03 决策）
        /// </summary>
        [Authorize(Policy = PolicyConstants.DoctorOnly)]
        [HttpPut("{id}/print-completed")]
        [EnableRateLimiting("ApiCalls")]
        public override async Task<IActionResult> RecordPrint(
            Guid id,
            [FromBody] RecordPrintRequest request, CancellationToken ct)
        {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await _medicalCaseCommandService.RecordPrintAsync(
                id, request.PrintType, request.PrinterName, operatorId, operatorName, ct);

            if (!result.IsSuccess)
                return HandleResult(result, useAuthMapping: true);

            _logger.LogInformation("打印记录写入成功，MedicalCaseId: {Id}, PrintType: {PrintType}, Operator: {Operator}",
                id, request.PrintType, operatorName);
            return Success(true, "打印记录已写入");
        }

        #region 状态流转（从 MedicalCaseProcessingController 合入）

        /// <summary>
        /// 更新医案状态（P1-10 2026-08-14: Completed 分支路由移入 StateService.UpdateStatus 统一处理——
        /// API 单一职能，Controller 仅编排）
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] MedicalCaseStatusInputDto request, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // T4-B9 + P1-10: 统一走 StateService 状态机校验（含 Completed→CompleteAsync 统一分派）
            var entity = await _medicalCaseStateService.UpdateStatusAsync(
                id, request.Status, operatorId, isAdmin, ct);
            if (entity == null)
                return HandleResult(Result<MedicalCaseDetailDto>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在"), useAuthMapping: true);

            var dto = _medicalCaseMapper.MapToMedicalCaseDetailDto(entity);
            LogOperation("更新医案状态", request, id);
            return Success(dto, "状态更新成功");
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed——P1-11 2026-08-14: 权限判断改方法级 Authorize，
        /// 强制关闭仅限 Admin/SuperAdmin——Doctor 无 force-close 权限 US-MC-012）
        /// P2-12-4 评估：Doctor 完成自己医案走 CompleteAsync（Doctor 权限），此 close 为 Admin 强制关闭（跳过工作流），11-business-flows 与 13b 已对齐。
        /// </summary>
        [Authorize(Policy = PolicyConstants.AdminOrSuperAdmin)]
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> CloseMedicalCase(Guid id, CancellationToken ct)
        {
            var (operatorId, _, _) = GetOperator();

            // 直接调用 StateService 关闭医案
            var entity = await _medicalCaseStateService.CompleteAsync(id, operatorId, isAdmin: true, skipWorkflowValidation: true, cancellationToken: ct);
            if (entity == null)
                return HandleResult(Result<MedicalCaseDetailDto>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在"), useAuthMapping: true);

            LogOperation("关闭医案", null, id);
            return Success("医案已关闭");
        }

        /// <summary>
        /// 挂起医案
        /// </summary>
        [HttpPut("{id}/suspend")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        public async Task<IActionResult> Suspend(
            Guid id,
            [FromBody] ConsultationInputDto? request = null, CancellationToken ct = default)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // 直接调用 StateService 挂起医案
            var entity = await _medicalCaseStateService.SuspendAsync(id, request, operatorId, isAdmin, ct);
            if (entity == null)
                return HandleResult(Result<MedicalCaseDetailDto>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在"), useAuthMapping: true);

            LogOperation("暂存医案", request, id);
            return Success("医案已暂存");
        }

        /// <summary>
        /// 取消医案（统一为软删除 + 审计日志）
        /// </summary>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> CancelMedicalCase(
            Guid id,
            [FromBody] CancelMedicalCaseRequest? request = null, CancellationToken ct = default)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            // 直接调用 StateService 取消医案
            var entity = await _medicalCaseStateService.CancelAsync(id, operatorId, isAdmin, request?.Reason, ct);
            if (entity == null)
                return HandleResult(Result<bool>.Failure(ErrorCode.MedicalCaseNotFound, "医案不存在"), useAuthMapping: true);

            _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
LogOperation("取消医案", null, id);
            return Success(true, "医案已取消");
        }

        #endregion
    }
}
