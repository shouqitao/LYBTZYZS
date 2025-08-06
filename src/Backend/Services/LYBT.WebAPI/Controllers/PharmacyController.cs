using Asp.Versioning;
using LYBT.Module.Pharmacy.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Pharmacy;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药房 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PharmacyController : BaseController {
        private readonly IPharmacyService _pharmacyService;
        /// <summary>
        /// 构造方法，注入药房服务
        /// </summary>
        public PharmacyController(IPharmacyService pharmacyService, IMemoryCache cache, ILogger<PharmacyController> logger) 
            : base(logger, cache) {
            _pharmacyService = pharmacyService;
        }

        /// <summary>
        /// 获取待抓药的处方列表
        /// </summary>
        [HttpGet("waiting")]
        public async Task<ActionResult<List<PharmacyDto>>> GetWaitingList() {
            try {
                var list = await _pharmacyService.GetWaitingListAsync();
                return Ok(list);
            } catch (Exception ex) {
                return HandleException(ex, "获取待抓药处方列表");
            }
        }

        /// <summary>
        /// 获取药房单列表 (RESTful GET /Pharmacy) - 支持模糊查询和分页
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<PaginatedResult<PharmacyDto>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null,
            [FromQuery] string? patientName = null,
            [FromQuery] string? doctorName = null,
            [FromQuery] PharmacyStatus? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] bool? needDecoction = null) {
            try {
                // 如果没有任何查询条件且请求第一页，返回简单列表
                if (page == 1 && pageSize >= 20 && string.IsNullOrEmpty(keyword) && string.IsNullOrEmpty(patientName) && 
                    string.IsNullOrEmpty(doctorName) && !status.HasValue && !startDate.HasValue && 
                    !endDate.HasValue && !needDecoction.HasValue) {
                    
                    var list = await _pharmacyService.GetListAsync();
                    var totalCount = list?.Count ?? 0;
                    var pagedList = list?.Take(pageSize).ToList() ?? new List<PharmacyDto>();
                    var result = new PaginatedResult<PharmacyDto> {
                        TotalCount = totalCount,
                        Items = pagedList
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
                var pagedResult = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(pagedResult);
            } catch (Exception ex) {
                return HandleException(ex, "获取药房单列表");
            }
        }

        /// <summary>
        /// 分页获取药房单列表
        /// </summary>
        [HttpGet("paged")]
        public async Task<ActionResult<PaginatedResult<PharmacyDto>>> GetPagedList([FromQuery] LYBT.Shared.Models.Common.PaginationRequest query) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (_, _, operatorRole) = GetOperator();
                var result = await _pharmacyService.GetPagedAsync(query, operatorRole);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "分页获取药房单列表");
            }
        }

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyDetailDto>> GetById(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var detail = await _pharmacyService.GetByIdAsync(id);
                if (detail == null) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }
                return Ok(detail);
            } catch (Exception ex) {
                return HandleException(ex, "获取药房单详情", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<PharmacyDto>> Add([FromBody] PharmacyCreateDto pharmacyCreateDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.AddAsync(pharmacyCreateDto);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "新增药房单失败" });
                }

                LogOperation("新增药房单成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "新增药房单");
            }
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<object>> Update([FromBody] PharmacyEditDto pharmacyEditDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.UpdateAsync(pharmacyEditDto);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "编辑药房单失败" });
                }

                // 获取更新后的资源
                var updated = await _pharmacyService.GetByIdAsync(pharmacyEditDto.Id);
                LogOperation("编辑药房单成功", updated, pharmacyEditDto.Id);
                return Ok(updated);
            } catch (Exception ex) {
                return HandleException(ex, "编辑药房单", new { PharmacyId = pharmacyEditDto.Id });
            }
        }

        /// <summary>
        /// 删除药房单
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<object>> Delete(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.DeleteAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }

                LogOperation("删除药房单成功", null, id);
                return NoContent();
            } catch (Exception ex) {
                return HandleException(ex, "删除药房单", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 标记处方为已抓药
        /// </summary>
        [HttpPost("{id}/prepared")]
        public async Task<ActionResult<object>> MarkAsPrepared(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.MarkAsPreparedAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在" });
                }

                LogOperation("标记处方为已抓药成功", null, id);
                return Ok(new { message = "标记为已抓药成功" });
            } catch (Exception ex) {
                return HandleException(ex, "标记处方为已抓药", new { PharmacyId = id });
            }
        }

        // ==================== 现场取药功能 API ====================

        /// <summary>
        /// 从处方创建药房单
        /// </summary>
        [HttpPost("from-prescription/{prescriptionId}")]
        public async Task<ActionResult<PharmacyDto>> CreateFromPrescription(Guid prescriptionId) {
            try {
                var validationResult = ValidateGuid(prescriptionId, "处方ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.CreateFromPrescriptionAsync(prescriptionId, operatorId, operatorName);
                if (result == null) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "从处方创建药房单失败" });
                }

                LogOperation("从处方创建药房单成功", result, result.Id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "从处方创建药房单", new { PrescriptionId = prescriptionId });
            }
        }

        /// <summary>
        /// 开始配药
        /// </summary>
        [HttpPost("{id}/start-dispensing")]
        public async Task<ActionResult<object>> StartDispensing(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.StartDispensingAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在或状态不允许开始配药" });
                }

                LogOperation("开始配药成功", null, id);
                return Ok(new { message = "开始配药成功" });
            } catch (Exception ex) {
                return HandleException(ex, "开始配药", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 完成配药
        /// </summary>
        [HttpPost("{id}/complete-dispensing")]
        public async Task<ActionResult<object>> CompleteDispensing(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.CompleteDispensingAsync(id);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在或状态不允许完成配药" });
                }

                LogOperation("完成配药成功", null, id);
                return Ok(new { message = "完成配药成功" });
            } catch (Exception ex) {
                return HandleException(ex, "完成配药", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 发药确认
        /// </summary>
        [HttpPost("{id}/confirm-dispense")]
        public async Task<ActionResult<object>> ConfirmDispense(Guid id, [FromBody] DispenseConfirmDto confirmDto) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;
                
                if (confirmDto == null || string.IsNullOrEmpty(confirmDto.ReceiverName)) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "接收人姓名不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.ConfirmDispenseAsync(id, confirmDto.ReceiverName, confirmDto.ReceiverPhone ?? "");
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在或状态不允许发药" });
                }

                LogOperation("发药确认成功", confirmDto, id);
                return Ok(new { message = "发药确认成功" });
            } catch (Exception ex) {
                return HandleException(ex, "发药确认", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 批量配药
        /// </summary>
        [HttpPost("batch-dispense")]
        public async Task<ActionResult<object>> BatchDispense([FromBody] BatchDispenseDto batchDto) {
            try {
                var validationResult = ValidateModel();
                if (validationResult != null) return validationResult;

                if (batchDto?.PharmacyIds == null || !batchDto.PharmacyIds.Any()) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "药房单ID列表不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.BatchDispenseAsync(batchDto.PharmacyIds, operatorId, operatorName);
                if (!result) {
                    return BadRequest(new ProblemDetails { Title = "操作失败", Detail = "批量配药处理失败" });
                }

                LogOperation("批量配药成功", batchDto, null);
                return Ok(new { message = $"批量配药成功，处理了 {batchDto.PharmacyIds.Count} 个药房单" });
            } catch (Exception ex) {
                return HandleException(ex, "批量配药");
            }
        }

        /// <summary>
        /// 获取今日统计
        /// </summary>
        [HttpGet("today-statistics")]
        public async Task<ActionResult<PharmacyTodayStatDto>> GetTodayStatistics() {
            try {
                var result = await _pharmacyService.GetTodayStatisticsAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取今日统计");
            }
        }

        /// <summary>
        /// 获取药材配置明细
        /// </summary>
        [HttpGet("{id}/herb-details")]
        public async Task<ActionResult<List<HerbDispenseDetailDto>>> GetHerbDispenseDetails(Guid id) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                var result = await _pharmacyService.GetHerbDispenseDetailsAsync(id);
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取药材配置明细", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 提交配药结果
        /// </summary>
        [HttpPost("{id}/submit-dispense-result")]
        public async Task<ActionResult<object>> SubmitDispenseResult(Guid id, [FromBody] List<HerbDispenseResultDto> results) {
            try {
                var validationResult = ValidateGuid(id, "药房单ID");
                if (validationResult != null) return validationResult;

                if (results == null || !results.Any()) {
                    return BadRequest(new ProblemDetails { Title = "请求参数错误", Detail = "配药结果不能为空" });
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _pharmacyService.SubmitDispenseResultAsync(id, results, operatorId, operatorName);
                if (!result) {
                    return NotFound(new ProblemDetails { Title = "资源未找到", Detail = "药房单不存在或提交失败" });
                }

                LogOperation("提交配药结果成功", results, id);
                return Ok(new { message = "提交配药结果成功" });
            } catch (Exception ex) {
                return HandleException(ex, "提交配药结果", new { PharmacyId = id });
            }
        }

        /// <summary>
        /// 获取待配药列表
        /// </summary>
        [HttpGet("pending")]
        public async Task<ActionResult<List<PharmacyQueueDto>>> GetPendingList() {
            try {
                var result = await _pharmacyService.GetPendingListAsync();
                return Ok(result);
            } catch (Exception ex) {
                return HandleException(ex, "获取待配药列表");
            }
        }
    }

    /// <summary>
    /// 发药确认DTO
    /// </summary>
    public class DispenseConfirmDto {
        public string ReceiverName { get; set; } = string.Empty;
        public string? ReceiverPhone { get; set; }
    }

    /// <summary>
    /// 批量配药DTO
    /// </summary>
    public class BatchDispenseDto {
        public List<Guid> PharmacyIds { get; set; } = new();
    }
}