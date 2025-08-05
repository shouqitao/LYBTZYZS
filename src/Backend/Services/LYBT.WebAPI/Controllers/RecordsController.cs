using Asp.Versioning;
using LYBT.Module.Records.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Records;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 病历 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/Records")]
    [Authorize]
    public class RecordsController : BaseController {
        private readonly IRecordService _recordService;
        /// <summary>
        /// 构造方法，注入病历服务
        /// </summary>
        public RecordsController(IRecordService recordService, IMemoryCache cache, ILogger<RecordsController> logger) 
            : base(logger, cache) {
            _recordService = recordService;
        }

        /// <summary>
        /// 获取病历列表 (RESTful GET /Records) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<RecordDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] string? diagnosis = null,
            [FromQuery] string? chiefComplaint = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? isShared = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && string.IsNullOrEmpty(diagnosis) && string.IsNullOrEmpty(chiefComplaint) &&
                    !startDate.HasValue && !endDate.HasValue && !isShared.HasValue) {
                    
                    var list = await _recordService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<RecordDto>();
                    var result = new PaginatedResult<RecordDto> {
                        TotalCount = totalCount,
                        Items = pagedList,
                        CurrentPage = page,
                        PageSize = pageSize
                    };
                    return Ok(result);
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _recordService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取病历列表");
            }
        }

        /// <summary>
        /// 分页查询病历列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<RecordDto>>> GetPaged([FromBody] PaginationRequest query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _recordService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页查询病历");
            }
        }

        /// <summary>
        /// 根据患者ID获取病历列表
        /// </summary>
        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<RecordDto>>> GetByPatient(Guid patientId) {
            try {
                var validationResult = ValidateGuid(patientId, "患者ID");
                if (validationResult != null) return validationResult;

                var list = await _recordService.GetByPatientIdAsync(patientId);
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取患者病历", new { PatientId = patientId });
            }
        }

        /// <summary>
        /// 获取病历详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RecordDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "病历ID");
                if (validationResult != null) return validationResult;

                var detail = await _recordService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "病历不存在", Status = 404 });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取病历详情", new { RecordId = id });
            }
        }

        /// <summary>
        /// 新增病历
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RecordDto>> Add([FromBody] RecordCreateDto recordCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.AddAsync(recordCreateDto, operatorId, operatorName);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "新增病历失败", Status = 400 });
                }

                LogOperation("新增病历成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增病历");
            }
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] RecordEditDto recordEditDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.UpdateAsync(recordEditDto, operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "编辑病历失败", Status = 400 });
                }

                // 获取更新后的资源
                var updated = await _recordService.GetByIdAsync(recordEditDto.Id);
                LogOperation("编辑病历成功", updated, recordEditDto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑病历", new { RecordId = recordEditDto.Id });
            }
        }

        /// <summary>
        /// 删除病历
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "病历ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _recordService.DeleteAsync(id, operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "病历不存在", Status = 404 });
                }

                LogOperation("删除病历成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除病历", new { RecordId = id });
            }
        }

        /// <summary>
        /// 标记病历为共享
        /// </summary>
        [HttpPost("share/{id}")]
        public async Task<ActionResult<object>> MarkAsShared(Guid id, [FromBody] List<string> doctorIds) {
            try {
                var validationResult = ValidateGuid(id, "病历ID");
                if (validationResult != null) return validationResult;

                var result = await _recordService.MarkAsSharedAsync(id, doctorIds);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "病历不存在", Status = 404 });
                }

                LogOperation("病历共享设置成功", new { RecordId = id, DoctorIds = doctorIds }, id);
                return Ok(new { message = "病历共享设置成功" });
            } catch (Exception ex) {
                return HandleException(ex, "病历共享设置", new { RecordId = id });
            }
        }

        /// <summary>
        /// 取消病历共享
        /// </summary>
        [HttpPost("unshare/{id}")]
        public async Task<ActionResult<object>> RevokeSharing(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "病历ID");
                if (validationResult != null) return validationResult;

                var result = await _recordService.RevokeSharingAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "病历不存在", Status = 404 });
                }

                LogOperation("病历共享取消成功", null, id);
                return Ok(new { message = "病历共享取消成功" });
            } catch (Exception ex) {
                return HandleException(ex, "病历共享取消", new { RecordId = id });
            }
        }

        /// <summary>
        /// 获取共享给指定医生的病历列表
        /// </summary>
        [HttpGet("shared/{doctorId}")]
        public async Task<ActionResult<List<RecordDto>>> GetShared(Guid doctorId) {
            try {
                var validationResult = ValidateGuid(doctorId, "医生ID");
                if (validationResult != null) return validationResult;

                var list = await _recordService.GetSharedRecordsAsync(doctorId);
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取共享病历", new { DoctorId = doctorId });
            }
        }
    }
}