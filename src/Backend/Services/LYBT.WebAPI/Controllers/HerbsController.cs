using Asp.Versioning;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
// Temporary: Keep using local DTOs until service layer mapping is updated
using LYBT.Models.Herbs;
using SharedHerbDetailDto = LYBT.Shared.Models.Contracts.Herbs.HerbDetailDto;
using SharedHerbPagedQueryDto = LYBT.Shared.Models.Contracts.Herbs.HerbPagedQueryDto;
using SharedHerbCreateDto = LYBT.Shared.Models.Contracts.Herbs.HerbCreateDto;
using SharedHerbUpdateDto = LYBT.Shared.Models.Contracts.Herbs.HerbUpdateDto;
using LocalHerbDetailDto = LYBT.Models.Herbs.HerbDetailDto;
using LocalHerbPagedQueryDto = LYBT.Models.Herbs.HerbPagedQueryDto;
using LocalHerbCreateDto = LYBT.Models.Herbs.HerbCreateDto;
using LYBT.Module.Herbs.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药材管理 API 控制器 - 统一前后端接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class HerbsController : ControllerBase {
        private readonly IHerbService _herbService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HerbsController> _logger;

        /// <summary>
        /// 构造方法，注入药材服务
        /// </summary>
        public HerbsController(IHerbService herbService, IMemoryCache cache, ILogger<HerbsController> logger) {
            _herbService = herbService;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取当前操作者信息
        /// </summary>
        private (Guid operatorId, string operatorName, UserRole operatorRole) GetOperator() {
            var userId = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User?.Identity?.Name;
            var roleStr = User?.FindFirst(ClaimTypes.Role)?.Value;

            if (Guid.TryParse(userId, out var opId) && !string.IsNullOrEmpty(userName)) {
                var role = Enum.TryParse<UserRole>(roleStr, out var parsedRole) ? parsedRole : UserRole.Staff;
                return (opId, userName, role);
            }
            throw new UnauthorizedAccessException("未登录或用户信息无效");
        }

        /// <summary>
        /// 映射本地DTO到共享DTO
        /// </summary>
        private SharedHerbDetailDto MapToSharedDto(HerbDto localDto) {
            return new SharedHerbDetailDto {
                Id = localDto.Id,
                Name = localDto.Name,
                Pinyin = localDto.Pinyin,
                WuBi = localDto.WuBi,
                Origin = localDto.Origin,
                Spec = localDto.Spec,
                Unit = localDto.Unit,
                Price = localDto.Price,
                Stock = localDto.Stock,
                BatchNo = localDto.BatchNo,
                ExpireDate = localDto.ExpireDate,
                Effect = localDto.Effect,
                Remark = "", // HerbDto中没有此字段，设为空
                Status = LYBT.Shared.Models.Enums.HerbStatus.Active, // 默认状态
                CreateTime = DateTime.Now, // 本地DTO没有此字段，设为当前时间
                UpdateTime = DateTime.Now, // 本地DTO没有此字段，设为当前时间
                IsActive = true // 默认为活跃状态
            };
        }

        /// <summary>
        /// 映射本地详情DTO到共享DTO
        /// </summary>
        private SharedHerbDetailDto MapToSharedDto(LocalHerbDetailDto localDto) {
            return new SharedHerbDetailDto {
                Id = localDto.Id,
                Name = localDto.Name,
                Pinyin = localDto.Pinyin,
                WuBi = "", // 本地DTO没有此字段，设为空
                Origin = localDto.Origin,
                Spec = localDto.Spec,
                Unit = localDto.Unit,
                Price = localDto.Price,
                Stock = localDto.Stock,
                BatchNo = localDto.BatchNo,
                ExpireDate = localDto.ExpireDate,
                Effect = localDto.Effect,
                Remark = localDto.Remark,
                Status = LYBT.Shared.Models.Enums.HerbStatus.Active, // 默认状态
                CreateTime = DateTime.Now, // 本地DTO没有此字段，设为当前时间
                UpdateTime = DateTime.Now, // 本地DTO没有此字段，设为当前时间
                IsActive = true // 默认为活跃状态
            };
        }

        /// <summary>
        /// 映射共享分页查询DTO到本地DTO
        /// </summary>
        private LocalHerbPagedQueryDto MapToLocalPagedQuery(SharedHerbPagedQueryDto sharedDto) {
            return new LocalHerbPagedQueryDto {
                Page = sharedDto.CurrentPage,
                PageSize = sharedDto.PageSize,
                Keyword = $"{sharedDto.Name} {sharedDto.Pinyin}".Trim(),
                Status = sharedDto.Status.HasValue ? 
                    (LYBT.Common.Enums.Herbs.HerbStatus?)sharedDto.Status : null,
                IncludeInactive = sharedDto.IncludeInactive,
                OnlyLowStock = sharedDto.OnlyLowStock,
                LowStockThreshold = sharedDto.LowStockThreshold,
                OnlyExpiring = sharedDto.OnlyExpiring,
                ExpiringDays = sharedDto.ExpiringDaysThreshold
            };
        }

        /// <summary>
        /// 映射共享创建DTO到本地DTO
        /// </summary>
        private LocalHerbCreateDto MapToLocalCreateDto(SharedHerbCreateDto sharedDto) {
            return new LocalHerbCreateDto {
                Name = sharedDto.Name,
                Pinyin = sharedDto.Pinyin,
                WuBi = sharedDto.WuBi,
                Origin = sharedDto.Origin,
                Spec = decimal.TryParse(sharedDto.Spec, out var spec) ? spec : 1,
                Unit = sharedDto.Unit,
                Price = sharedDto.Price,
                Stock = sharedDto.Stock,
                BatchNo = sharedDto.BatchNo,
                ExpireDate = sharedDto.ExpireDate,
                Effect = sharedDto.Effect,
                Remark = sharedDto.Remark
            };
        }

        /// <summary>
        /// 映射共享更新DTO到本地DTO
        /// </summary>
        private HerbEditDto MapToLocalEditDto(SharedHerbUpdateDto sharedDto) {
            return new HerbEditDto {
                Id = sharedDto.Id,
                Name = sharedDto.Name,
                Pinyin = sharedDto.Pinyin,
                WuBi = sharedDto.WuBi,
                Origin = sharedDto.Origin,
                Spec = decimal.TryParse(sharedDto.Spec, out var spec) ? spec : 1,
                Unit = sharedDto.Unit,
                Price = sharedDto.Price,
                Stock = sharedDto.Stock,
                BatchNo = sharedDto.BatchNo,
                ExpireDate = sharedDto.ExpireDate,
                Effect = sharedDto.Effect,
                Remark = sharedDto.Remark,
                Status = (LYBT.Common.Enums.Herbs.HerbStatus)sharedDto.Status
            };
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SharedHerbDetailDto>>>> GetList() {
            try {
                // 缓存药材列表
                const string cacheKey = "herbs_list";
                if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                    list = await _herbService.GetListAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(ApiResponse<List<SharedHerbDetailDto>>.Success(list?.Select(MapToSharedDto).ToList() ?? new List<SharedHerbDetailDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材列表失败");
                return StatusCode(500, ApiResponse<List<SharedHerbDetailDto>>.Fail("获取药材列表失败", 500));
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<ApiResponse<PaginatedResult<SharedHerbDetailDto>>>> GetPaged([FromBody] SharedHerbPagedQueryDto query) {
            try {
                if (query.CurrentPage <= 0)
                    query.CurrentPage = 1;
                if (query.PageSize <= 0 || query.PageSize > 100)
                    query.PageSize = 20;

                // 直接使用共享DTO调用服务
                var result = await _herbService.GetPagedAsync(query);
                var mappedResult = new PaginatedResult<SharedHerbDetailDto>(
                    result.Items.Select(MapToSharedDto).ToList(),
                    result.TotalCount,
                    result.CurrentPage,
                    result.PageSize
                );
                return Ok(ApiResponse<PaginatedResult<SharedHerbDetailDto>>.Success(mappedResult));
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询药材失败");
                return StatusCode(500, ApiResponse<PaginatedResult<SharedHerbDetailDto>>.Fail("分页查询药材失败", 500));
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<SharedHerbDetailDto>>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<SharedHerbDetailDto>.Fail("药材ID不能为空", 400));
                }

                var detail = await _herbService.GetByIdAsync(id);
                if (detail == null)
                    return NotFound(ApiResponse<SharedHerbDetailDto>.Fail("药材不存在", 404));
                return Ok(ApiResponse<SharedHerbDetailDto>.Success(detail));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材详情失败，ID: {HerbId}", id);
                return StatusCode(500, ApiResponse<SharedHerbDetailDto>.Fail("获取药材详情失败", 500));
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<object>>> Add([FromBody] SharedHerbCreateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _herbService.AddAsync(dto);
                if (!result)
                    return BadRequest(ApiResponse<object>.Fail("新增药材失败", 400));

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                return Ok(ApiResponse<object>.Success("新增药材成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "新增药材失败");
                return StatusCode(500, ApiResponse<object>.Fail("新增药材失败", 500));
            }
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        [HttpPut]
        public async Task<ActionResult<ApiResponse<object>>> Update([FromBody] SharedHerbUpdateDto dto) {
            try {
                if (!ModelState.IsValid) {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(ApiResponse<object>.Fail($"参数验证失败：{errors}", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _herbService.UpdateAsync(dto);
                if (!result)
                    return BadRequest(ApiResponse<object>.Fail("编辑药材失败", 400));

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{dto.Id}");

                return Ok(ApiResponse<object>.Success("编辑药材成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑药材失败，ID: {HerbId}", dto.Id);
                return StatusCode(500, ApiResponse<object>.Fail("编辑药材失败", 500));
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest(ApiResponse<object>.Fail("药材ID不能为空", 400));
                }

                var (operatorId, operatorName, _) = GetOperator();
                var result = await _herbService.DeleteAsync(id);
                if (!result)
                    return NotFound(ApiResponse<object>.Fail("药材不存在", 404));

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{id}");

                return Ok(ApiResponse<object>.Success("删除药材成功"));
            } catch (Exception ex) {
                _logger.LogError(ex, "删除药材失败，ID: {HerbId}", id);
                return StatusCode(500, ApiResponse<object>.Fail("删除药材失败", 500));
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        [HttpPost("import")]
        public async Task<ActionResult> Import([FromBody] List<HerbImportDto> dtos) {
            try {
                if (dtos == null || dtos.Count == 0) {
                    return BadRequest("导入数据不能为空");
                }

                var count = await _herbService.ImportAsync(dtos);

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                return Ok(new { Imported = count, Message = $"成功导入 {count} 个药材" });
            } catch (Exception ex) {
                _logger.LogError(ex, "批量导入药材失败");
                return StatusCode(500, "批量导入药材失败");
            }
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        [HttpPost("export")]
        public async Task<ActionResult<List<SharedHerbDetailDto>>> Export() {
            try {
                var data = await _herbService.ExportAsync();
                return Ok(data);
            } catch (Exception ex) {
                _logger.LogError(ex, "导出药材数据失败");
                return StatusCode(500, "导出药材数据失败");
            }
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        [HttpPatch("status")]
        public async Task<ActionResult> UpdateStatus([FromBody] HerbStatusUpdateDto dto) {
            try {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _herbService.UpdateStatusAsync(dto);
                if (!result)
                    return NotFound("药材不存在");

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{dto.Id}");

                return Ok("药材状态更新成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "更新药材状态失败，ID: {HerbId}", dto.Id);
                return StatusCode(500, "更新药材状态失败");
            }
        }

        /// <summary>
        /// 批量更新药材状态
        /// </summary>
        [HttpPatch("batch-status")]
        public async Task<ActionResult> BatchUpdateStatus([FromBody] HerbBatchStatusUpdateDto dto) {
            try {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (dto.Ids == null || dto.Ids.Count == 0) {
                    return BadRequest("请选择要更新的药材");
                }

                var count = await _herbService.BatchUpdateStatusAsync(dto);

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                foreach (var id in dto.Ids) {
                    _cache.Remove($"herb_detail_{id}");
                }

                return Ok(new { UpdatedCount = count, Message = $"成功更新 {count} 个药材状态" });
            } catch (Exception ex) {
                _logger.LogError(ex, "批量更新药材状态失败");
                return StatusCode(500, "批量更新药材状态失败");
            }
        }

        /// <summary>
        /// 根据状态获取药材列表
        /// </summary>
        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<HerbDto>>> GetByStatus(HerbStatus status) {
            try {
                var list = await _herbService.GetByStatusAsync(status);
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "根据状态获取药材列表失败，状态: {Status}", status);
                return StatusCode(500, "根据状态获取药材列表失败");
            }
        }

        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<ApiResponse<List<HerbDto>>>> GetAvailable() {
            try {
                // 缓存可用药材列表
                const string cacheKey = "active_herbs";
                if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                    list = await _herbService.GetAvailableHerbsAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(15));
                }
                return Ok(ApiResponse<List<HerbDto>>.Success(list ?? new List<HerbDto>()));
            } catch (Exception ex) {
                _logger.LogError(ex, "获取可用药材列表失败");
                return StatusCode(500, ApiResponse<List<HerbDto>>.Fail("获取可用药材列表失败", 500));
            }
        }

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        [HttpGet("out-of-stock")]
        public async Task<ActionResult<List<HerbDto>>> GetOutOfStock() {
            try {
                var list = await _herbService.GetOutOfStockHerbsAsync();
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取缺货药材列表失败");
                return StatusCode(500, "获取缺货药材列表失败");
            }
        }

        /// <summary>
        /// 获取即将过期药材列表
        /// </summary>
        [HttpGet("expiring")]
        public async Task<ActionResult<List<HerbDto>>> GetExpiring([FromQuery] int days = 30) {
            try {
                if (days <= 0)
                    days = 30;
                var list = await _herbService.GetExpiringHerbsAsync(days);
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取即将过期药材列表失败");
                return StatusCode(500, "获取即将过期药材列表失败");
            }
        }

        /// <summary>
        /// 检查并更新过期药材
        /// </summary>
        [HttpPost("check-expired")]
        public async Task<ActionResult> CheckExpired() {
            try {
                var count = await _herbService.CheckAndUpdateExpiredHerbsAsync();

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                return Ok(new { UpdatedCount = count, Message = $"检查完成，更新了 {count} 个过期药材" });
            } catch (Exception ex) {
                _logger.LogError(ex, "检查过期药材失败");
                return StatusCode(500, "检查过期药材失败");
            }
        }

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<Dictionary<HerbStatus, int>>> GetStatistics() {
            try {
                var statistics = await _herbService.GetStatusStatisticsAsync();
                return Ok(statistics);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材状态统计失败");
                return StatusCode(500, "获取药材状态统计失败");
            }
        }
    }
}