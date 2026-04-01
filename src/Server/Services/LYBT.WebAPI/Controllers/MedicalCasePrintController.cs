using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 打印操作
    /// 从原MedicalCaseController拆分，专注于医案打印管理
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Tags("MedicalCases")]
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
        /// 记录打印完成 -- 更新打印管理字段并写入打印日志
        /// T2-X8-04~08: IsPrinted/PrintCount/LastPrintedAt/PrintVersion + PrintLog
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">打印完成请求</param>
        [HttpPut("{id}/print-completed")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseDetailDto>), 404)]
        public async Task<IActionResult> RecordPrintCompleted(
            Guid id,
            [FromBody] PrintCompletedRequest request)
        {
            var (operatorId, operatorName, _) = GetOperator();

            var result = await _facade.RecordPrintCompletedAsync(
                id, request.PrintType, operatorId, operatorName, request.PrinterName);

            if (result == null)
                return NotFound("医案不存在");

            var dto = _mapper.MapToMedicalCaseDetailDto(result);

            _logger.LogInformation("打印完成记录成功，MedicalCaseId: {Id}, PrintVersion: {Version}, PrintCount: {Count}",
                id, result.PrintVersion, result.PrintCount);
            return Success(dto, "打印记录更新成功");
        }

        /// <summary>
        /// 添加打印日志 -- 记录打印成功或失败
        /// T4-S5-02: 支持打印成功/失败日志记录
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="request">打印日志输入</param>
        [HttpPost("{id}/print-logs")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> AddPrintLog(
            Guid id,
            [FromBody] PrintLogInputDto request)
        {
            var (operatorId, operatorName, _) = GetOperator();

            var result = await _facade.AddPrintLogAsync(
                id, request.PrintType, request.IsSuccess,
                operatorId, operatorName,
                request.PrinterName, request.ErrorMessage);

            if (!result)
                return NotFound("医案不存在");

            _logger.LogInformation("打印日志记录成功，MedicalCaseId: {Id}, IsSuccess: {IsSuccess}",
                id, request.IsSuccess);
            return Success("打印日志记录成功");
        }
    }
}
