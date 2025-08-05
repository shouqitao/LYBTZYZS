using Asp.Versioning;
using LYBT.Module.Registration.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Registration;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {
    /// <summary>
    /// 挂号管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class RegistrationController : BaseController {
        private readonly IRegistrationService _registrationService;
        /// <summary>
        /// 构造方法，注入挂号服务
        /// </summary>
        public RegistrationController(IRegistrationService registrationService, IMemoryCache cache, ILogger<RegistrationController> logger) 
            : base(logger, cache) {
            _registrationService = registrationService;
        }


        /// <summary>
        /// 获取挂号列表 (RESTful GET /Registration) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<RegistrationDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] RegistrationType? registrationType = null,
            [FromQuery] RegistrationStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? isFromDoctor = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && !registrationType.HasValue && !status.HasValue &&
                    !startDate.HasValue && !endDate.HasValue && !isFromDoctor.HasValue) {
                    
                    var list = await _registrationService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<RegistrationDto>();
                    var result = new PaginatedResult<RegistrationDto> {
                        TotalCount = totalCount,
                        Items = pagedList,
                        CurrentPage = page,
                        PageSize = pageSize
                    };
                    return Ok(result);
                }

                // 使用分页查询服务 (简化版本，只保留基本搜索功能)
                var query = new LYBT.Shared.Models.Common.PaginationRequest {
                    CurrentPage = page,
                    PageSize = pageSize,
                    SearchKeyword = keyword
                };
                
                var (_, _, operatorRole) = GetOperator();
                var pagedResult = await _registrationService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取挂号列表失败");
            }
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<RegistrationDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "挂号ID");
                if (validationResult != null) return validationResult;

                var detail = await _registrationService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "挂号记录不存在", Status = 404 });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取挂号详情失败", new { RegistrationId = id });
            }
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<RegistrationDto>> Add([FromBody] RegistrationCreateDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.AddAsync(dto);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "新增挂号失败", Status = 400 });
                }

                LogOperation("新增挂号成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增挂号失败");
            }
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<object>> Update([FromBody] RegistrationEditDto dto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.UpdateAsync(dto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "参数错误", Detail = "编辑挂号失败", Status = 400 });
                }

                // 获取更新后的资源
                var updated = await _registrationService.GetByIdAsync(dto.Id);
                LogOperation("编辑挂号成功", updated, dto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑挂号失败", new { RegistrationId = dto.Id });
            }
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "挂号ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "挂号记录不存在", Status = 404 });
                }

                LogOperation("删除挂号成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除挂号失败", new { RegistrationId = id });
            }
        }

        /// <summary>
        /// 取消挂号（软删除）
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult<object>> Cancel(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "挂号ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _registrationService.CancelAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "挂号记录不存在", Status = 404 });
                }

                // 获取更新后的资源
                var updated = await _registrationService.GetByIdAsync(id);
                LogOperation("取消挂号成功", updated, id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "取消挂号失败", new { RegistrationId = id });
            }
        }

        /// <summary>
        /// 分页查询挂号列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<RegistrationDto>>> GetPaged([FromBody] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var (_, _, operatorRole) = GetOperator();
                var result = await _registrationService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页查询挂号失败");
            }
        }
    }
}