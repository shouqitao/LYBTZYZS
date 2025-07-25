using LYBT.Common.Enums.Herbs;
using LYBT.Common.Models;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Module.Herbs.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 药材管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/herbs")]
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
        /// 获取药材列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<HerbDto>>> GetList() {
            try {
                // 缓存药材列表
                const string cacheKey = "herbs_list";
                if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                    list = await _herbService.GetListAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(10));
                }
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材列表失败");
                return StatusCode(500, "获取药材列表失败");
            }
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PagedResultDto<HerbDto>>> GetPaged([FromBody] HerbPagedQueryDto query) {
            try {
                if (query.Page <= 0)
                    query.Page = 1;
                if (query.PageSize <= 0 || query.PageSize > 100)
                    query.PageSize = 20;

                var result = await _herbService.GetPagedAsync(query);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询药材失败");
                return StatusCode(500, "分页查询药材失败");
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<HerbDetailDto>> GetById(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest("药材ID不能为空");
                }

                var detail = await _herbService.GetByIdAsync(id);
                if (detail == null)
                    return NotFound("药材不存在");
                return Ok(detail);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取药材详情失败，ID: {HerbId}", id);
                return StatusCode(500, "获取药材详情失败");
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] HerbCreateDto dto) {
            try {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _herbService.AddAsync(dto);
                if (!result)
                    return BadRequest("新增药材失败");

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");

                return Ok("新增药材成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "新增药材失败");
                return StatusCode(500, "新增药材失败");
            }
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] HerbEditDto dto) {
            try {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _herbService.UpdateAsync(dto);
                if (!result)
                    return BadRequest("编辑药材失败");

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{dto.Id}");

                return Ok("编辑药材成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "编辑药材失败，ID: {HerbId}", dto.Id);
                return StatusCode(500, "编辑药材失败");
            }
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            try {
                if (id == Guid.Empty) {
                    return BadRequest("药材ID不能为空");
                }

                var result = await _herbService.DeleteAsync(id);
                if (!result)
                    return NotFound("药材不存在");

                // 清除缓存
                _cache.Remove("herbs_list");
                _cache.Remove("active_herbs");
                _cache.Remove($"herb_detail_{id}");

                return Ok("删除药材成功");
            } catch (Exception ex) {
                _logger.LogError(ex, "删除药材失败，ID: {HerbId}", id);
                return StatusCode(500, "删除药材失败");
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
        public async Task<ActionResult<List<HerbDetailDto>>> Export() {
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
        public async Task<ActionResult<List<HerbDto>>> GetAvailable() {
            try {
                // 缓存可用药材列表
                const string cacheKey = "active_herbs";
                if (!_cache.TryGetValue(cacheKey, out List<HerbDto>? list)) {
                    list = await _herbService.GetAvailableHerbsAsync();
                    _cache.Set(cacheKey, list, TimeSpan.FromMinutes(15));
                }
                return Ok(list);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取可用药材列表失败");
                return StatusCode(500, "获取可用药材列表失败");
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