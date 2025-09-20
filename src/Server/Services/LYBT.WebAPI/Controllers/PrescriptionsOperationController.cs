using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方业务操作 API 控制器 - 处理复制、快速保存、取消等业务操作
    /// 对应 IPrescriptionBusinessService 的业务功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/prescriptions/operation")]
    [Authorize]
    public class PrescriptionsOperationController : BaseApiController
    {
        private readonly IPrescriptionBusinessService _businessService;

        /// <summary>
        /// 构造方法，注入处方业务服务
        /// </summary>
        public PrescriptionsOperationController(
            IPrescriptionBusinessService businessService,
            IMemoryCache memoryCache,
            ILogger<PrescriptionsOperationController> logger)
            : base(logger, memoryCache)
        {
            _businessService = businessService;
        }

        /// <summary>
        /// 复制处方
        /// </summary>
        [HttpPost("{id:guid}/copy")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> Copy(Guid id, [FromBody] PrescriptionCopyDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrWhiteSpace(dto.NewName))
                {
                    return ValidationFail<PrescriptionDto>("新处方名称不能为空", "INVALID_NAME");
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _businessService.CopyAsync(id, dto.NewName, operatorId, operatorName);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "复制处方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("复制处方", new { OriginalId = id, NewName = dto.NewName }, result.Data.Id);
                return Success(result.Data, "处方复制成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "复制处方", new { id, dto });
            }
        }

        /// <summary>
        /// 复制患者最近处方
        /// </summary>
        [HttpPost("copy-last")]
        public async Task<ActionResult<ApiResponse<PrescriptionDto>>> CopyLastPrescription([FromBody] CopyLastPrescriptionDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<PrescriptionDto>("请求数据不能为空", "INVALID_REQUEST");
                }

                var validationResult = ValidateGuid<PrescriptionDto>(dto.PatientId, "患者ID");
                if (validationResult != null) return validationResult;

                validationResult = ValidateGuid<PrescriptionDto>(dto.DoctorId, "医生ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _businessService.CopyLastPrescriptionAsync(dto.PatientId, dto.DoctorId, operatorId, operatorName);
                
                if (!result.IsSuccess || result.Data == null)
                {
                    return BusinessFail<PrescriptionDto>(result.ErrorMessage ?? "复制最近处方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("复制患者最近处方", dto, result.Data.Id);
                return Success(result.Data, "最近处方复制成功");
            }
            catch (Exception ex)
            {
                return HandleException<PrescriptionDto>(ex, "复制患者最近处方", dto);
            }
        }

        /// <summary>
        /// 快速保存处方
        /// </summary>
        [HttpPost("{id:guid}/quick-save")]
        public async Task<ActionResult<ApiResponse<bool>>> QuickSave(Guid id, [FromBody] QuickPrescriptionDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ValidationFail<bool>("保存数据不能为空", "INVALID_DATA");
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _businessService.QuickSaveAsync(id, dto, operatorId, operatorName);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "快速保存失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("快速保存处方", dto, id);
                return Success(result.Data, "处方快速保存成功");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "快速保存处方", new { id, dto });
            }
        }

        /// <summary>
        /// 取消处方
        /// </summary>
        [HttpPost("{id:guid}/cancel")]
        public async Task<ActionResult<ApiResponse<bool>>> Cancel(Guid id, [FromBody] CancelPrescriptionDto dto)
        {
            try
            {
                var (operatorId, operatorName, _) = GetOperator();
                var result = await _businessService.CancelAsync(id, operatorId, operatorName);
                
                if (!result.IsSuccess)
                {
                    return BusinessFail<bool>(result.ErrorMessage ?? "取消处方失败", ApiErrorCodes.DATASAVEFAILED);
                }

                LogOperation("取消处方", new { Id = id, Reason = dto?.Reason }, id);
                return Success(result.Data, "处方已取消");
            }
            catch (Exception ex)
            {
                return HandleException<bool>(ex, "取消处方", new { id, dto });
            }
        }
    }

    /// <summary>
    /// 复制最近处方DTO
    /// </summary>
    public class CopyLastPrescriptionDto
    {
        /// <summary>
        /// 患者ID
        /// </summary>
        public Guid PatientId { get; set; }

        /// <summary>
        /// 医生ID
        /// </summary>
        public Guid DoctorId { get; set; }
    }

    /// <summary>
    /// 取消处方DTO
    /// </summary>
    public class CancelPrescriptionDto
    {
        /// <summary>
        /// 取消原因
        /// </summary>
        public string? Reason { get; set; }
    }
}