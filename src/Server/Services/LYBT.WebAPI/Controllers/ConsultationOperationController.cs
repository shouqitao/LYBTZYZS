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
    /// 看诊业务操作 API 控制器 - 处理开始看诊、保存四诊等业务操作
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
        /// 构造方法，注入看诊业务服务
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
        /// 开始看诊
        /// </summary>
        [HttpPost("start")]
        public async Task<ActionResult<ApiResponse<ConsultationDto>>> StartConsultation(
            [FromBody] ConsultationStartDto startDto)
        {
            try
            {
                if (startDto == null)
                {
                    return ValidationFail<ConsultationDto>("开始看诊数据不能为空", "INVALID_DATA");
                }

                var validationResult = ValidateModel<ConsultationDto>();
                if (validationResult != null) return validationResult;

                var result = await _businessService.StartAsync(startDto);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<ConsultationDto>(
                        result.ErrorMessage ?? "开始看诊失败", 
                        ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("开始看诊", startDto, result.Data.Id);
                return Success(result.Data, "看诊已开始");
            }
            catch (Exception ex)
            {
                return HandleException<ConsultationDto>(ex, "开始看诊", startDto);
            }
        }

        /// <summary>
        /// 保存中医四诊信息
        /// </summary>
        [HttpPost("{consultationId:guid}/four-diagnosis")]
        public async Task<ActionResult<ApiResponse<bool>>> SaveFourDiagnosis(
            Guid consultationId,
            [FromBody] FourDiagnosisDto fourDiagnosisData)
        {
            try
            {
                var validationResult = ValidateGuid<bool>(consultationId, "看诊记录ID");
                if (validationResult != null) return validationResult;

                if (fourDiagnosisData == null)
                {
                    return ValidationFail<bool>("四诊数据不能为空", "INVALID_DATA");
                }

                var result = await _businessService.SaveFourDiagnosisAsync(consultationId, fourDiagnosisData);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(
                        result.ErrorMessage ?? "保存四诊信息失败", 
                        ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("保存中医四诊信息", fourDiagnosisData, consultationId);
                return Success(result.Data, "四诊信息保存成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "保存四诊信息", new { consultationId, fourDiagnosisData });
            }
        }
    }

    /// <summary>
    /// 中医四诊信息DTO
    /// </summary>
    public class FourDiagnosisDto
    {
        /// <summary>
        /// 望诊信息
        /// </summary>
        public InspectionDto? Inspection { get; set; }

        /// <summary>
        /// 闻诊信息
        /// </summary>
        public AuscultationDto? Auscultation { get; set; }

        /// <summary>
        /// 问诊信息
        /// </summary>
        public InquiryDto? Inquiry { get; set; }

        /// <summary>
        /// 切诊信息
        /// </summary>
        public PalpationDto? Palpation { get; set; }
    }

    /// <summary>
    /// 望诊信息DTO
    /// </summary>
    public class InspectionDto
    {
        /// <summary>
        /// 神色
        /// </summary>
        public string? Spirit { get; set; }

        /// <summary>
        /// 面色
        /// </summary>
        public string? Complexion { get; set; }

        /// <summary>
        /// 形态
        /// </summary>
        public string? Physique { get; set; }

        /// <summary>
        /// 舌质
        /// </summary>
        public string? TongueBody { get; set; }

        /// <summary>
        /// 舌苔
        /// </summary>
        public string? TongueCoating { get; set; }
    }

    /// <summary>
    /// 闻诊信息DTO
    /// </summary>
    public class AuscultationDto
    {
        /// <summary>
        /// 声音
        /// </summary>
        public string? Voice { get; set; }

        /// <summary>
        /// 呼吸
        /// </summary>
        public string? Breathing { get; set; }

        /// <summary>
        /// 咳嗽
        /// </summary>
        public string? Cough { get; set; }

        /// <summary>
        /// 气味
        /// </summary>
        public string? Odor { get; set; }
    }

    /// <summary>
    /// 问诊信息DTO
    /// </summary>
    public class InquiryDto
    {
        /// <summary>
        /// 寒热
        /// </summary>
        public string? ColdHeat { get; set; }

        /// <summary>
        /// 汗液
        /// </summary>
        public string? Perspiration { get; set; }

        /// <summary>
        /// 饮食
        /// </summary>
        public string? Diet { get; set; }

        /// <summary>
        /// 大小便
        /// </summary>
        public string? Excretion { get; set; }

        /// <summary>
        /// 睡眠
        /// </summary>
        public string? Sleep { get; set; }

        /// <summary>
        /// 疼痛部位
        /// </summary>
        public string? PainLocation { get; set; }
    }

    /// <summary>
    /// 切诊信息DTO
    /// </summary>
    public class PalpationDto
    {
        /// <summary>
        /// 脉象
        /// </summary>
        public string? Pulse { get; set; }

        /// <summary>
        /// 脉率
        /// </summary>
        public int? PulseRate { get; set; }

        /// <summary>
        /// 腹诊
        /// </summary>
        public string? Abdominal { get; set; }

        /// <summary>
        /// 其他触诊
        /// </summary>
        public string? Other { get; set; }
    }
}