using Asp.Versioning;
using LYBT.Module.TreatmentRoom.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.TreatmentRoom;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 理疗室 API 控制器（现场理疗模式）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class TreatmentRoomController : BaseController {
        private readonly ITreatmentRoomService _treatmentRoomService;

        /// <summary>
        /// 构造方法，注入理疗室服务
        /// </summary>
        public TreatmentRoomController(ITreatmentRoomService treatmentRoomService, IMemoryCache cache, ILogger<TreatmentRoomController> logger) 
            : base(logger, cache) {
            _treatmentRoomService = treatmentRoomService;
        }

        /// <summary>
        /// 获取治疗记录列表 (RESTful GET /TreatmentRoom) - 支持查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<TreatmentDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] Guid? patientId = null,
            [FromQuery] Guid? doctorId = null,
            [FromQuery] string? treatmentType = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && !patientId.HasValue && 
                    !doctorId.HasValue && string.IsNullOrEmpty(treatmentType) && string.IsNullOrEmpty(status) && 
                    !startDate.HasValue && !endDate.HasValue) {
                    
                    var list = await _treatmentRoomService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<TreatmentDto>();
                    var result = new PaginatedResult<TreatmentDto> {
                        TotalCount = totalCount,
                        Items = pagedList
                    };
                    return Ok(result);
                }

                // 使用分页查询服务
                var query = new TreatmentQueryDto {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword,
                    PatientId = patientId,
                    DoctorId = doctorId,
                    TreatmentType = treatmentType,
                    Status = status,
                    StartDate = startDate,
                    EndDate = endDate
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _treatmentRoomService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取治疗记录列表");
            }
        }

        /// <summary>
        /// 获取治疗记录详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TreatmentDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                var detail = await _treatmentRoomService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗记录不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取治疗记录详情", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 新增治疗记录
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TreatmentDetailDto>> Create([FromBody] TreatmentCreateDto treatmentCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.CreateAsync(treatmentCreateDto, operatorId, operatorName);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增治疗记录失败" });
                }

                LogOperation("新增治疗记录成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增治疗记录");
            }
        }

        /// <summary>
        /// 编辑治疗记录
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TreatmentDetailDto>> Update(Guid id, [FromBody] TreatmentUpdateDto treatmentUpdateDto) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;
                
                var modelValidationResult = ValidateModel();
                if (modelValidationResult != null) return modelValidationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.UpdateAsync(id, treatmentUpdateDto, operatorId, operatorName);
                if (result == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗记录不存在" });
                }

                LogOperation("编辑治疗记录成功", result, id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "编辑治疗记录", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 删除治疗记录
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.DeleteAsync(id, operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗记录不存在" });
                }

                LogOperation("删除治疗记录成功", null, id);
                return Ok(new { message = "删除治疗记录成功" });
            } catch (Exception ex) {
                return HandleException(ex, "删除治疗记录", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 根据状态获取治疗记录
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<TreatmentDto>>> GetByStatus(string status) {
            try {
                var list = await _treatmentRoomService.GetByStatusAsync(status);
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "按状态获取治疗记录列表", new { Status = status });
            }
        }

        // ==================== 现场理疗功能 API ====================

        /// <summary>
        /// 获取治疗队列
        /// </summary>
        [HttpGet("queue")]
        public async Task<ActionResult<List<TreatmentQueueDto>>> GetTreatmentQueue() {
            try {
                var result = await _treatmentRoomService.GetTreatmentQueueAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取治疗队列");
            }
        }

        /// <summary>
        /// 开始治疗
        /// </summary>
        [HttpPost("{id}/start")]
        public async Task<ActionResult<object>> StartTreatment(Guid id, [FromBody] StartTreatmentDto startDto) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                if (startDto == null || string.IsNullOrEmpty(startDto.TherapistName)) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "治疗师姓名不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.StartTreatmentAsync(id, startDto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "开始治疗失败，可能状态不正确" });
                }

                LogOperation("开始治疗成功", startDto, id);
                return Ok(new { message = "开始治疗成功" });
            } catch (Exception ex) {
                return HandleException(ex, "开始治疗", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 完成治疗
        /// </summary>
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<object>> CompleteTreatment(Guid id, [FromBody] CompleteTreatmentDto completeDto) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.CompleteTreatmentAsync(id, completeDto ?? new(), operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "完成治疗失败，可能状态不正确" });
                }

                LogOperation("完成治疗成功", completeDto, id);
                return Ok(new { message = "完成治疗成功" });
            } catch (Exception ex) {
                return HandleException(ex, "完成治疗", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 取消治疗
        /// </summary>
        [HttpPost("{id}/cancel")]
        public async Task<ActionResult<object>> CancelTreatment(Guid id, [FromBody] CancelTreatmentDto cancelDto) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                if (cancelDto == null || string.IsNullOrEmpty(cancelDto.Reason)) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "取消原因不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.CancelTreatmentAsync(id, cancelDto.Reason, operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "取消治疗失败" });
                }

                LogOperation("取消治疗成功", cancelDto, id);
                return Ok(new { message = "取消治疗成功" });
            } catch (Exception ex) {
                return HandleException(ex, "取消治疗", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 获取理疗室状态
        /// </summary>
        [HttpGet("room-status")]
        public async Task<ActionResult<List<TreatmentRoomStatusDto>>> GetRoomStatus() {
            try {
                var result = await _treatmentRoomService.GetRoomStatusAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取理疗室状态");
            }
        }

        /// <summary>
        /// 获取今日统计
        /// </summary>
        [HttpGet("today-statistics")]
        public async Task<ActionResult<TodayTreatmentStatDto>> GetTodayStatistics() {
            try {
                var result = await _treatmentRoomService.GetTodayStatisticsAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取今日统计");
            }
        }

        /// <summary>
        /// 获取统计数据
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<TreatmentStatisticsDto>> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null) {
            try {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1).AddSeconds(-1);
                
                var result = await _treatmentRoomService.GetStatisticsAsync(start, end);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取统计数据");
            }
        }

        /// <summary>
        /// 从挂号创建治疗记录
        /// </summary>
        [HttpPost("from-registration/{registrationId}")]
        public async Task<ActionResult<TreatmentDetailDto>> CreateFromRegistration(Guid registrationId, [FromBody] CreateFromRegistrationDto dto) {
            try {
                var validationResult = ValidateGuid(registrationId, "挂号ID");
                if (validationResult != null) return validationResult;

                if (dto == null || string.IsNullOrEmpty(dto.TreatmentType)) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "治疗类型不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.CreateFromRegistrationAsync(registrationId, dto.TreatmentType, operatorId, operatorName);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "从挂号创建治疗记录失败" });
                }

                LogOperation("从挂号创建治疗记录成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "从挂号创建治疗记录", new { RegistrationId = registrationId });
            }
        }

        /// <summary>
        /// 分配治疗室
        /// </summary>
        [HttpPost("{id}/assign-room")]
        public async Task<ActionResult<object>> AssignRoom(Guid id, [FromBody] AssignRoomDto assignDto) {
            try {
                var validationResult = ValidateGuid(id, "治疗记录ID");
                if (validationResult != null) return validationResult;

                if (assignDto?.RoomNumber == null || assignDto.RoomNumber <= 0) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "房间号不能为空且必须大于0" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.AssignRoomAsync(id, assignDto.RoomNumber, operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "治疗记录不存在" });
                }

                LogOperation("分配治疗室成功", assignDto, id);
                return Ok(new { message = $"分配治疗室{assignDto.RoomNumber}成功" });
            } catch (Exception ex) {
                return HandleException(ex, "分配治疗室", new { TreatmentId = id });
            }
        }

        /// <summary>
        /// 获取可用治疗师
        /// </summary>
        [HttpGet("therapists")]
        public async Task<ActionResult<List<TherapistDto>>> GetAvailableTherapists() {
            try {
                var result = await _treatmentRoomService.GetAvailableTherapistsAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取可用治疗师");
            }
        }

        /// <summary>
        /// 批量安排治疗
        /// </summary>
        [HttpPost("batch-schedule")]
        public async Task<ActionResult<object>> BatchScheduleTreatments([FromBody] BatchScheduleDto batchDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                if (batchDto?.TreatmentIds == null || !batchDto.TreatmentIds.Any()) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "治疗记录ID列表不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _treatmentRoomService.BatchScheduleTreatmentsAsync(
                    batchDto.TreatmentIds, batchDto.TherapistId, batchDto.TherapistName, operatorId, operatorName);
                
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "批量安排治疗失败" });
                }

                LogOperation("批量安排治疗成功", batchDto, null);
                return Ok(new { message = $"批量安排治疗成功，处理了 {batchDto.TreatmentIds.Count} 个治疗记录" });
            } catch (Exception ex) {
                return HandleException(ex, "批量安排治疗");
            }
        }
    }

    /// <summary>
    /// 取消治疗DTO
    /// </summary>
    public class CancelTreatmentDto {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// 从挂号创建治疗记录DTO
    /// </summary>
    public class CreateFromRegistrationDto {
        public string TreatmentType { get; set; } = string.Empty;
    }

    /// <summary>
    /// 分配治疗室DTO
    /// </summary>
    public class AssignRoomDto {
        public int RoomNumber { get; set; }
    }

    /// <summary>
    /// 批量安排DTO
    /// </summary>
    public class BatchScheduleDto {
        public List<Guid> TreatmentIds { get; set; } = new();
        public Guid TherapistId { get; set; }
        public string TherapistName { get; set; } = string.Empty;
    }
}