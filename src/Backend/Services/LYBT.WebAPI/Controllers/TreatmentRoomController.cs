using Asp.Versioning;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 治疗室 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TreatmentRoomController : BaseController {
        private readonly ITreatmentRoomService _treatmentRoomService;

        /// <summary>
        /// 构造方法，注入治疗室服务
        /// </summary>
        public TreatmentRoomController(ITreatmentRoomService treatmentRoomService, IMemoryCache cache, ILogger<TreatmentRoomController> logger) 
            : base(logger, cache) {
            _treatmentRoomService = treatmentRoomService;
        }

        /// <summary>
        /// 获取治疗室单列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<TreatmentRoomDto>>> GetList() {
            try {
                var list = await _treatmentRoomService.GetListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取治疗室列表");
            }
        }

        /// <summary>
        /// 分页获取治疗室列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<TreatmentRoomDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _treatmentRoomService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取治疗室列表");
            }
        }

        /// <summary>
        /// 获取治疗室单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TreatmentRoomDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "治疗室ID");
                if (validationResult != null) return validationResult;

                var detail = await _treatmentRoomService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗室记录不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取治疗室详情", new { TreatmentRoomId = id });
            }
        }

        /// <summary>
        /// 新增治疗室单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> Add([FromBody] TreatmentRoomCreateDto treatmentRoomCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _treatmentRoomService.AddAsync(treatmentRoomCreateDto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增治疗室单失败" });
                }

                LogOperation("新增治疗室成功", treatmentRoomCreateDto, null);
                return Ok(new { message = "新增治疗室单成功" });
            } catch (Exception ex) {
                return HandleException(ex, "新增治疗室");
            }
        }

        /// <summary>
        /// 编辑治疗室单
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] TreatmentRoomEditDto treatmentRoomEditDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var result = await _treatmentRoomService.UpdateAsync(treatmentRoomEditDto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑治疗室单失败" });
                }

                // 获取更新后的资源
                var updated = await _treatmentRoomService.GetByIdAsync(treatmentRoomEditDto.Id);
                LogOperation("编辑治疗室单成功", updated, treatmentRoomEditDto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑治疗室", new { TreatmentRoomId = treatmentRoomEditDto.Id });
            }
        }

        /// <summary>
        /// 删除治疗室单
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "治疗室ID");
                if (validationResult != null) return validationResult;

                var result = await _treatmentRoomService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗室记录不存在" });
                }

                LogOperation("删除治疗室成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除治疗室", new { TreatmentRoomId = id });
            }
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<TreatmentRoomDto>>> GetByStatus(string status) {
            try {
                var list = await _treatmentRoomService.GetByStatusAsync(status);
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "按状态获取治疗室列表", new { Status = status });
            }
        }
    }
}