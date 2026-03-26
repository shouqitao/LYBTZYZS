using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例打印管理 API V1
    /// 职责：处方标记、打印记录、打印日志管理
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCasePrintController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCasePrintController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCasePrintController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// 动态流程控制
        /// </summary>
        [HttpPut("{id}/prescription-flag")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 422)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 403)]
        public async Task<IActionResult> SetPrescriptionFlag(
            Guid id,
            [FromBody] SetPrescriptionFlagRequest request,
            CancellationToken cancellationToken = default)
        {
            var (operatorId, _, operatorRole) = GetOperator();
            var isAdmin = operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin;

            var result = await _facade.SetPrescriptionFlagAsync(id, request.NeedsPrescription, operatorId, isAdmin, cancellationToken);
            if (result == null)
            {
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));
            }

            var dto = _mapper.MapToMedicalCaseDetailDto(result);
            _logger.LogInformation("处方标记更新成功，MedicalCaseId: {Id}, NeedsPrescription: {Flag}",
                id, request.NeedsPrescription);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "处方标记更新成功"));
        }

        /// <summary>
        /// 记录打印完成 -- 更新打印管理字段并写入打印日志
        /// 更新IsPrinted/PrintCount/LastPrintedAt/PrintVersion + PrintLog
        /// </summary>
        [HttpPut("{id}/print-completed")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> RecordPrintCompleted(
            Guid id,
            [FromBody] PrintCompletedRequest request,
            CancellationToken cancellationToken = default)
        {
            var (operatorId, operatorName, _) = GetOperator();

            var result = await _facade.RecordPrintCompletedAsync(
                id, request.PrintType, operatorId, operatorName, request.PrinterName, cancellationToken);

            if (result == null)
                return NotFound(ApiResponse<MedicalCaseDetailDto>.CreateFail("医案不存在"));

            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("打印完成记录成功，MedicalCaseId: {Id}, PrintVersion: {Version}, PrintCount: {Count}",
                id, result.PrintVersion, result.PrintCount);
            return Ok(ApiResponse<MedicalCaseDetailDto>.CreateSuccess(dto, "打印记录更新成功"));
        }

        /// <summary>
        /// 添加打印日志 -- 记录打印成功或失败
        /// 支持打印成功/失败日志记录
        /// </summary>
        [HttpPost("{id}/print-logs")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> AddPrintLog(
            Guid id,
            [FromBody] PrintLogInputDto request,
            CancellationToken cancellationToken = default)
        {
            var (operatorId, operatorName, _) = GetOperator();

            var result = await _facade.AddPrintLogAsync(
                id, request.PrintType, request.IsSuccess,
                operatorId, operatorName,
                request.PrinterName, request.ErrorMessage,
                cancellationToken);

            if (!result)
                return NotFound(ApiResponse<object>.CreateFail("医案不存在"));

            _logger.LogInformation("打印日志记录成功，MedicalCaseId: {Id}, IsSuccess: {IsSuccess}",
                id, request.IsSuccess);
            return Ok(ApiResponse<object>.CreateSuccess(null, "打印日志记录成功"));
        }
    }

}
