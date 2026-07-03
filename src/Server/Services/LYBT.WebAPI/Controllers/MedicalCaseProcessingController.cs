using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Application.Commands;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 工作流操作
    /// 从原MedicalCaseController拆分，专注于医案状态流转和工作流操作
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Tags("MedicalCases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCaseProcessingController : BaseApiController
    {
        private readonly ISender _sender;

        public MedicalCaseProcessingController(
            ISender sender,
            ILogger<MedicalCaseProcessingController> logger)
            : base(logger)
        {
            _sender = sender;
        }

        /// <summary>
        /// 更新医案状态
        /// 支持 Draft/Active/Completed 状态流转（Cancelled 已移除，使用 IsDeleted 替代）
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] MedicalCaseStatusInputDto request)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            if (request.Status == MedicalCaseStatus.Completed)
            {
                var completeResult = await _sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin));
                if (!completeResult.IsSuccess)
                    return NotFound(completeResult.Error ?? "医案不存在");

                return Success("医案已完成");
            }

            var result = await _sender.Send(new UpdateMedicalCaseStatusCommand(id, request.Status, operatorId, isAdmin));

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}",
                id, request.Status);
            return Success(result.Value!, "状态更新成功");
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.1
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> CloseMedicalCase(Guid id)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _sender.Send(new CompleteMedicalCaseCommand(id, operatorId, isAdmin));

            if (!result.IsSuccess)
                return NotFound(result.Error ?? "医案不存在");

            _logger.LogInformation("医案关闭，MedicalCaseId: {Id}", id);
            return Success("医案已关闭");
        }

        /// <summary>
        /// 挂起医案
        /// 挂起医案，设置状态为Suspended，不触发完成验证
        /// 资源级权限由 Service 层 EnsureCanEdit/EnsureCanDelete 统一检查
        /// </summary>
        [HttpPut("{id}/suspend")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        public async Task<IActionResult> Suspend(
            Guid id,
            [FromBody] ConsultationInputDto? request = null)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new SuspendMedicalCaseCommand(id, operatorId, isAdmin));
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("医案暂存成功，MedicalCaseId: {Id}", id);
            return Success("医案已暂存");
        }

        /// <summary>
        /// 取消医案（统一为软删除 + 审计日志）
        /// 端点保留供客户端调用，内部行为从 CaseStatus=Cancelled 改为 IsDeleted=true
        /// </summary>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(ApiResponse), 404)]
        [ProducesResponseType(typeof(ApiResponse), 403)]
        public async Task<IActionResult> CancelMedicalCase(
            Guid id,
            [FromBody] CancelMedicalCaseRequestDto? request = null)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _sender.Send(new CancelMedicalCaseCommand(id, operatorId, isAdmin, request?.Reason));
            if (!result.IsSuccess)
            {
                return NotFound(result.Error ?? "医案不存在");
            }

            _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
            return Success(true, "医案已取消");
        }
    }

}


