using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Module.MedicalCases.Application.Queries;
using LYBT.Module.MedicalCases.Controllers;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

using SetPrescriptionFlagRequest = LYBT.Shared.Models.Contracts.MedicalCase.SetPrescriptionFlagRequest;
using RecordPrintRequest = LYBT.Shared.Models.Contracts.MedicalCase.RecordPrintRequest;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 继承 BaseMedicalCasesController 提供标准方法
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Tags("MedicalCases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCasesController : BaseMedicalCasesController
    {
        public MedicalCasesController(
            ISender sender,
            ILogger<MedicalCasesController> logger)
            : base(sender, logger)
        {
        }

        /// <summary>
        /// 查询医案列表（分页）- 添加 OutputCache
        /// </summary>
        [HttpGet]
        [OutputCache(PolicyName = "MedicalCaseCache")]
        public new async Task<IActionResult> GetList(
            [FromQuery] MedicalCaseStatus? status = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool includeAllDoctors = false,
            [FromQuery] string? keyword = null,
            CancellationToken ct = default)
        {
            if (ValidatePagination(page, pageSize) is { } error) return error;
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole is UserRole.SuperAdmin or UserRole.Admin || includeAllDoctors;
            var result = await Sender.Send(new GetMedicalCasesQuery(
                status, patientId, page, pageSize, operatorId, isAdmin, keyword), ct);

            if (!result.IsSuccess)
                return BusinessFail(result.Error ?? "查询失败");

            return Success(result.Value!, "查询成功");
        }

        /// <summary>
        /// 创建新医案 - 添加 OutputCache 和 RateLimiting
        /// </summary>
        [HttpPost]
        [EnableRateLimiting("ApiCalls")]
        [Authorize(Policy = PolicyConstants.DoctorOrAdminOrReceptionist)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public override async Task<IActionResult> Create([FromBody] MedicalCaseInputDto dto, CancellationToken ct)
        {
            var (doctorId, _, _) = GetOperator();

            dto.Id = null;
            var result = await Sender.Send(new CreateMedicalCaseCommand(dto, doctorId), ct);

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "患者不存在");

            var responseDto = result.Value!;

            _logger.LogInformation("医案创建成功，ID: {Id}, Doctor: {DoctorName}, Patient: {PatientName}",
                responseDto.Id, responseDto.DoctorName, responseDto.PatientName);

            return CreatedAtAction(nameof(GetById),
                new { id = responseDto.Id, version = ApiVersionConstants.V1 },
                ApiResponse<MedicalCaseDetailDto>.CreateSuccess(responseDto, "医案创建成功"));
        }

        /// <summary>
        /// 保存医案聚合根 - 添加 OutputCache 和 RateLimiting
        /// </summary>
        [HttpPut("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 400)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public new async Task<IActionResult> Save(
            Guid id,
            [FromBody] MedicalCaseInputDto request, CancellationToken ct)
        {
            if (request.Id != id)
            {
                return Error("请求ID与路由ID不一致");
            }

            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await Sender.Send(new SaveMedicalCaseCommand(request, operatorId, isAdmin), ct);

            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("医案聚合保存成功，MedicalCaseId: {MedicalCaseId}", id);
            return Success(result.Value!, "保存成功");
        }

        /// <summary>
        /// 删除医案（软删除）- 添加 OutputCache 和 RateLimiting
        /// </summary>
        [HttpDelete("{id:guid}")]
        [EnableRateLimiting("ApiCalls")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public override async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await Sender.Send(new DeleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案已软删除，MedicalCaseId: {Id}, OperatorId: {OperatorId}", id, operatorId);
            return Success(true, "医案已删除");
        }

        /// <summary>
        /// 批量删除医案 - 添加 OutputCache 和 RateLimiting
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

            var result = await Sender.Send(new BatchDeleteMedicalCasesCommand(dto.Ids, operatorId, isAdmin), ct);
            if (!result.IsSuccess || result.Value == null)
            {
                return BusinessFail(result.Error ?? "批量删除失败");
            }

            LogOperation("批量删除医案", new { Ids = dto.Ids, Result = result.Value.Message }, null);
            return Success(result.Value, result.Value.Message);
        }

        /// <summary>
        /// 标记是否需要开处方 - 添加 RateLimiting
        /// </summary>
        [HttpPut("{id:guid}/prescription-flag")]
        [EnableRateLimiting("ApiCalls")]
        public override async Task<IActionResult> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await Sender.Send(new SetPrescriptionFlagCommand(id, request.NeedsPrescription, operatorId, isAdmin), ct);
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                id, request.NeedsPrescription);
            return Success(result.Value!, "处方标记更新成功");
        }

        /// <summary>
        /// 记录打印完成 - 添加 RateLimiting
        /// </summary>
        [HttpPut("{id:guid}/print-completed")]
        [EnableRateLimiting("ApiCalls")]
        public override async Task<IActionResult> RecordPrint(
            Guid id,
            [FromBody] RecordPrintRequest request, CancellationToken ct)
        {
            var (operatorId, operatorName, _) = GetOperator();
            var result = await Sender.Send(new RecordPrintCommand(
                id, request.PrintType, request.PrinterName, operatorId, operatorName), ct);

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("打印记录写入成功，MedicalCaseId: {Id}, PrintType: {PrintType}, Operator: {Operator}",
                id, request.PrintType, operatorName);
            return Success(true, "打印记录已写入");
        }

        #region 状态流转（从 MedicalCaseProcessingController 合入）

        /// <summary>
        /// 更新医案状态
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

            if (request.Status == MedicalCaseStatus.Completed)
            {
                var completeResult = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
                if (!completeResult.IsSuccess)
                    return NotFound(completeResult.Error ?? "医案不存在");
                return Success("医案已完成");
            }

            var result = await Sender.Send(new UpdateMedicalCaseStatusCommand(id, request.Status, operatorId, isAdmin), ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}", id, request.Status);
            return Success(result.Value!, "状态更新成功");
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// </summary>
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> CloseMedicalCase(Guid id, CancellationToken ct)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await Sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin), ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案关闭，MedicalCaseId: {Id}", id);
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

            var result = await Sender.Send(new SuspendMedicalCaseCommand(id, operatorId, isAdmin), ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案暂存成功，MedicalCaseId: {Id}", id);
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
            [FromBody] CancelMedicalCaseRequestDto? request = null, CancellationToken ct = default)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await Sender.Send(new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason), ct);
            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
            return Success(true, "医案已取消");
        }

        #endregion
    }
}
