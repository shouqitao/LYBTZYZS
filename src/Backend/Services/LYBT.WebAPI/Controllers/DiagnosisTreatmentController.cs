using Asp.Versioning;
using LYBT.Module.DiagnosisTreatment.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 诊疗 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class DiagnosisTreatmentController : BaseController {
        private readonly IDiagnosisTreatmentService _diagnosisTreatmentService;
        /// <summary>
        /// 构造方法，注入诊疗服务
        /// </summary>
        public DiagnosisTreatmentController(IDiagnosisTreatmentService diagnosisTreatmentService, IMemoryCache cache, ILogger<DiagnosisTreatmentController> logger) 
            : base(logger, cache) {
            _diagnosisTreatmentService = diagnosisTreatmentService;
        }

        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DiagnosisTreatmentDto>>> GetList() {
            try {
                var list = await _diagnosisTreatmentService.GetListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取诊疗列表");
            }
        }

        /// <summary>
        /// 分页获取诊疗列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<DiagnosisTreatmentDto>>> GetPagedList([FromQuery] PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _diagnosisTreatmentService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取诊疗列表");
            }
        }

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DiagnosisTreatmentDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "诊疗ID");
                if (validationResult != null) return validationResult;

                var detail = await _diagnosisTreatmentService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "诊疗记录不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取诊疗详情", new { DiagnosisTreatmentId = id });
            }
        }

        /// <summary>
        /// 新增诊疗
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<DiagnosisTreatmentDto>> Add([FromBody] DiagnosisTreatmentCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.AddAsync(dto);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增诊疗失败" });
                }

                LogOperation("新增诊疗成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增诊疗");
            }
        }

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<DiagnosisTreatmentDto>> Update([FromBody] DiagnosisTreatmentEditDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑诊疗失败" });
                }

                // 获取更新后的资源
                var updated = await _diagnosisTreatmentService.GetByIdAsync(dto.Id);
                LogOperation("编辑诊疗成功", updated, dto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑诊疗", new { DiagnosisTreatmentId = dto.Id });
            }
        }

        /// <summary>
        /// 删除诊疗
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "诊疗ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _diagnosisTreatmentService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "诊疗记录不存在" });
                }

                LogOperation("删除诊疗成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除诊疗", new { DiagnosisTreatmentId = id });
            }
        }
    }
}