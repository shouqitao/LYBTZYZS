using Asp.Versioning;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例工作流 API V1
    /// 职责：医案状态流转、挂起、关闭、取消等工作流操作
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCaseWorkflowController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCaseWorkflowController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCaseWorkflowController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        /// <summary>
        /// 更新医案状态
        /// 支持 Draft/Active/Completed 状态流转
        /// </summary>
        [HttpPut("{id}/status")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        public async Task<IActionResult> UpdateStatus(
            Guid id,
            [FromBody] UpdateStatusRequest request)
        {
            // Completed 状态通过 CompleteAsync 统一入口处理
            if (request.Status == MedicalCaseStatus.Completed)
            {
                var (operatorId, _, operatorRole) = GetOperator();
                var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
                var completeResult = await _facade.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation: false);
                if (completeResult == null)
                    return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

                var completeDto = _mapper.MapToMedicalCaseDto(completeResult);
                return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(completeDto, "医案已完成"));
            }

            var result = await _facade.UpdateStatusAsync(id, request.Status);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDto(result);
            _logger.LogInformation("医案状态更新成功，MedicalCaseId: {Id}, NewStatus: {Status}",
                id, request.Status);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "状态更新成功"));
        }

        /// <summary>
        /// 关闭医案（直接标记为Completed）
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        [HttpPut("{id}/close")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> CloseMedicalCase(Guid id)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;
            var result = await _facade.CompleteAsync(id, operatorId, isAdmin, skipWorkflowValidation: true);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDetailDto(result);
            _logger.LogInformation("医案关闭，MedicalCaseId: {Id}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "医案已关闭"));
        }

        /// <summary>
        /// 挂起医案
        /// 挂起医案，设置状态为Suspended，不触发完成验证
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

            var result = await _facade.SuspendAsync(id, request, operatorId, isAdmin);
            if (result == null)
            {
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));
            }

            var dto = _mapper.MapToMedicalCaseDto(result);
            _logger.LogInformation("医案暂存成功，MedicalCaseId: {Id}", id);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "医案已暂存"));
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
            [FromBody] CancelMedicalCaseRequest? request = null)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.CancelAsync(id, operatorId, isAdmin, request?.Reason);
            if (result == null)
            {
                return NotFound(ApiResponse.CreateFail("医案不存在"));
            }

            _logger.LogInformation("医案取消成功(软删除)，MedicalCaseId: {Id}", id);
            return NoContent();
        }
    }

}
