using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例业务操作 API 控制器 - 处理创建关联处方、打印等业务操作
    /// 对应 IMedicalCaseBusinessService 的业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medical-case/operation")]
    [Authorize]
    public class MedicalCaseOperationController : BaseApiController
    {
        private readonly IMedicalCaseBusinessService _businessService;

        /// <summary>
        /// 构造方法，注入医疗案例业务服务
        /// </summary>
        public MedicalCaseOperationController(
            IMedicalCaseBusinessService businessService,
            IMemoryCache memoryCache,
            ILogger<MedicalCaseOperationController> logger)
            : base(logger, memoryCache)
        {
            _businessService = businessService;
        }

        /// <summary>
        /// 创建医疗案例并关联处方
        /// 在单个短事务中创建医案和可选的关联处方
        /// </summary>
        [HttpPost("create-with-prescription")]
        public async Task<ActionResult<ApiResponse<MedicalCaseWithPrescriptionResultDto>>> CreateWithPrescription(
            [FromBody] MedicalCaseWithPrescriptionCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return ValidationFail<MedicalCaseWithPrescriptionResultDto>("创建数据不能为空", "INVALID_DATA");
                }

                var validationResult = ValidateModel<MedicalCaseWithPrescriptionResultDto>();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _businessService.CreateWithPrescriptionAsync(createDto, operatorId, operatorName);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<MedicalCaseWithPrescriptionResultDto>(
                        result.ErrorMessage ?? "创建医疗案例失败", 
                        ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("创建医疗案例并关联处方", createDto, result.Data.MedicalCase?.Id);
                return Success(result.Data, "医疗案例创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<MedicalCaseWithPrescriptionResultDto>(ex, "创建医疗案例并关联处方", createDto);
            }
        }

        /// <summary>
        /// 打印病历记录
        /// </summary>
        [HttpPost("{caseId:guid}/print")]
        public async Task<ActionResult<ApiResponse<object>>> PrintMedicalRecord(
            Guid caseId, 
            [FromBody] PrintOptionsDto printOptions)
        {
            try
            {
                var validationResult = ValidateGuid<object>(caseId, "医疗案例ID");
                if (validationResult != null) return validationResult;

                var result = await _businessService.PrintMedicalRecordAsync(caseId, printOptions ?? new PrintOptionsDto());
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<object>(
                        result.ErrorMessage ?? "打印病历失败", 
                        ApiErrorCodes.DATAQUERYFAILED);
                }

                LogOperation("打印病历记录", new { CaseId = caseId, Options = printOptions }, caseId);
                return Success(result.Data ?? new object(), "病历记录准备就绪");
            }
            catch (Exception ex)
            {
                return HandleException<object>(ex, "打印病历记录", new { caseId, printOptions });
            }
        }
    }

    /// <summary>
    /// 打印选项DTO
    /// </summary>
    public class PrintOptionsDto
    {
        /// <summary>
        /// 是否包含处方信息
        /// </summary>
        public bool IncludePrescription { get; set; } = true;

        /// <summary>
        /// 是否包含诊断信息
        /// </summary>
        public bool IncludeDiagnosis { get; set; } = true;

        /// <summary>
        /// 是否包含检查结果
        /// </summary>
        public bool IncludeExamination { get; set; } = true;

        /// <summary>
        /// 打印格式
        /// </summary>
        public string Format { get; set; } = "PDF";
    }
}