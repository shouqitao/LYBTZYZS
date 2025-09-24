using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 诊疗业务操作 API 控制器 - 处理开始诊疗、保存四诊等业务操作
    /// 对应 IConsultationBusinessService 的业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/consultation/operation")]
    [Authorize]
    public class ConsultationOperationController : BaseApiController
    {
        private readonly IConsultationBusinessService _businessService;

        /// <summary>
        /// 构造方法，注入诊疗业务服务
        /// </summary>
        public ConsultationOperationController(
            IConsultationBusinessService businessService,
            IMemoryCache memoryCache,
            ILogger<ConsultationOperationController> logger)
            : base(logger, memoryCache)
        {
            _businessService = businessService;
        }

        /// <summary>
        /// 开始诊疗
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> StartConsultation(
            [FromBody] ConsultationStartDto startDto)
        {
            try
            {
                if (startDto == null)
                {
                    return ValidationFail<ConsultationDto>("开始诊疗数据不能为空", "INVALID_DATA");
                }

                var validationResult = ValidateModel<ConsultationDto>();
                if (validationResult != null) return validationResult;

                var result = await _businessService.StartAsync(startDto);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ConsultationDto>(
                        result.ErrorMessage ?? "开始诊疗失败", 
                        ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("开始诊疗", startDto, result.Data.Id);
                return Success(result.Data, "诊疗已开始");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "开始诊疗", startDto);
            }
        }

    }
}