using LYBT.Infrastructure.Configuration;
using LYBT.Infrastructure.Configuration.Dtos;
using LYBT.Shared.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 统一配置管理API控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UnifiedConfigController : ControllerBase {
        private readonly IUnifiedConfigService _configService;
        private readonly ILogger<UnifiedConfigController> _logger;

        public UnifiedConfigController(IUnifiedConfigService configService, ILogger<UnifiedConfigController> logger) {
            _configService = configService;
            _logger = logger;
        }

        // ==================== 全局设置管理 ====================

        /// <summary>
        /// 获取全局设置
        /// </summary>
        /// <returns>全局设置</returns>
        [HttpGet("global-settings")]
        public async Task<ActionResult<GlobalSettingsDto>> GetGlobalSettings() {
            try {
                var settings = await _configService.GetGlobalSettingsAsync();
                if (settings == null) {
                    return NotFound("全局设置不存在");
                }
                return Ok(settings);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取全局设置失败");
                return StatusCode(500, "获取全局设置失败");
            }
        }

        /// <summary>
        /// 更新全局设置
        /// </summary>
        /// <param name="globalSettingsDto">全局设置对象</param>
        /// <returns>更新结果</returns>
        [HttpPut("global-settings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> UpdateGlobalSettings([FromBody] GlobalSettingsDto globalSettingsDto) {
            try {
                // TODO: 从当前用户上下文获取用户信息
                var currentUserId = Guid.NewGuid(); // 临时使用
                var currentUserName = "Admin"; // 临时使用

                var result = await _configService.UpdateGlobalSettingsAsync(globalSettingsDto, currentUserId, currentUserName);
                if (result) {
                    return Ok(new { Message = "全局设置更新成功" });
                }
                return BadRequest("全局设置更新失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "更新全局设置失败");
                return StatusCode(500, "更新全局设置失败");
            }
        }

        // ==================== 系统设置管理 ====================

        /// <summary>
        /// 获取设置值
        /// </summary>
        /// <param name="key">设置键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>设置值</returns>
        [HttpGet("settings/{key}")]
        public async Task<ActionResult<string>> GetSetting(string key, [FromQuery] string? defaultValue = null) {
            try {
                var value = await _configService.GetSettingAsync(key, defaultValue);
                return Ok(new { Key = key, Value = value });
            } catch (Exception ex) {
                _logger.LogError(ex, "获取设置失败: {Key}", key);
                return StatusCode(500, "获取设置失败");
            }
        }

        /// <summary>
        /// 设置配置值
        /// </summary>
        /// <param name="request">设置请求</param>
        /// <returns>设置结果</returns>
        [HttpPost("settings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SetSetting([FromBody] SetSettingRequest request) {
            try {
                // TODO: 从当前用户上下文获取用户ID
                var currentUserId = Guid.NewGuid(); // 临时使用

                var result = await _configService.SetSettingAsync(
                    request.Key,
                    request.Value,
                    request.Description,
                    request.Group,
                    currentUserId);

                if (result) {
                    return Ok(new { Message = "设置成功" });
                }
                return BadRequest("设置失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "设置配置失败: {Key}", request.Key);
                return StatusCode(500, "设置配置失败");
            }
        }

        /// <summary>
        /// 批量设置配置值
        /// </summary>
        /// <param name="settings">设置字典</param>
        /// <returns>设置结果</returns>
        [HttpPost("settings/batch")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SetSettings([FromBody] Dictionary<string, object> settings) {
            try {
                // TODO: 从当前用户上下文获取用户ID
                var currentUserId = Guid.NewGuid(); // 临时使用

                var result = await _configService.SetSettingsAsync(settings, currentUserId);
                if (result) {
                    return Ok(new { Message = $"成功设置 {settings.Count} 个配置项" });
                }
                return BadRequest("批量设置失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "批量设置配置失败");
                return StatusCode(500, "批量设置失败");
            }
        }

        /// <summary>
        /// 分页查询设置
        /// </summary>
        /// <param name="group">设置分组</param>
        /// <param name="keyword">关键词</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>分页设置结果</returns>
        [HttpGet("settings")]
        public async Task<ActionResult<PaginatedResult<SettingsDto>>> GetSettings(
            [FromQuery] string? group = null,
            [FromQuery] string? keyword = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10) {
            try {
                var result = await _configService.GetSettingsAsync(group, keyword, pageIndex, pageSize);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "查询设置失败");
                return StatusCode(500, "查询设置失败");
            }
        }

        /// <summary>
        /// 根据分组获取所有设置
        /// </summary>
        /// <param name="group">设置分组</param>
        /// <returns>设置字典</returns>
        [HttpGet("settings/group/{group}")]
        public async Task<ActionResult<Dictionary<string, string>>> GetSettingsByGroup(string group) {
            try {
                var settings = await _configService.GetSettingsByGroupAsync(group);
                return Ok(settings);
            } catch (Exception ex) {
                _logger.LogError(ex, "根据分组获取设置失败: {Group}", group);
                return StatusCode(500, "获取设置失败");
            }
        }

        /// <summary>
        /// 删除设置
        /// </summary>
        /// <param name="key">设置键</param>
        /// <returns>删除结果</returns>
        [HttpDelete("settings/{key}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteSetting(string key) {
            try {
                var result = await _configService.DeleteSettingAsync(key);
                if (result) {
                    return Ok(new { Message = "设置删除成功" });
                }
                return BadRequest("设置删除失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "删除设置失败: {Key}", key);
                return StatusCode(500, "删除设置失败");
            }
        }

        // ==================== 诊断目录管理 ====================

        /// <summary>
        /// 获取所有诊断目录
        /// </summary>
        /// <returns>诊断目录列表</returns>
        [HttpGet("diagnosis-catalogs")]
        public async Task<ActionResult<List<DiagnosisCatalogDto>>> GetDiagnosisCatalogs() {
            try {
                var catalogs = await _configService.GetDiagnosisCatalogsAsync();
                return Ok(catalogs);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取诊断目录失败");
                return StatusCode(500, "获取诊断目录失败");
            }
        }

        /// <summary>
        /// 分页查询诊断目录
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <param name="isEnabled">是否启用</param>
        /// <param name="pageIndex">页码</param>
        /// <param name="pageSize">页大小</param>
        /// <returns>分页诊断目录结果</returns>
        [HttpGet("diagnosis-catalogs/paged")]
        public async Task<ActionResult<PaginatedResult<DiagnosisCatalogDto>>> GetDiagnosisCatalogsPaged(
            [FromQuery] string? keyword = null,
            [FromQuery] bool? isEnabled = null,
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10) {
            try {
                var result = await _configService.GetDiagnosisCatalogsAsync(keyword, isEnabled, pageIndex, pageSize);
                return Ok(result);
            } catch (Exception ex) {
                _logger.LogError(ex, "分页查询诊断目录失败");
                return StatusCode(500, "查询诊断目录失败");
            }
        }

        /// <summary>
        /// 根据ID获取诊断目录
        /// </summary>
        /// <param name="id">诊断目录ID</param>
        /// <returns>诊断目录</returns>
        [HttpGet("diagnosis-catalogs/{id}")]
        public async Task<ActionResult<DiagnosisCatalogDto>> GetDiagnosisCatalog(Guid id) {
            try {
                var catalog = await _configService.GetDiagnosisCatalogByIdAsync(id);
                if (catalog == null) {
                    return NotFound("诊断目录不存在");
                }
                return Ok(catalog);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取诊断目录失败: {Id}", id);
                return StatusCode(500, "获取诊断目录失败");
            }
        }

        /// <summary>
        /// 创建诊断目录
        /// </summary>
        /// <param name="diagnosisCatalogDto">诊断目录对象</param>
        /// <returns>创建结果</returns>
        [HttpPost("diagnosis-catalogs")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult> CreateDiagnosisCatalog([FromBody] DiagnosisCatalogDto diagnosisCatalogDto) {
            try {
                // TODO: 从当前用户上下文获取用户ID
                var currentUserId = Guid.NewGuid(); // 临时使用

                var result = await _configService.CreateDiagnosisCatalogAsync(diagnosisCatalogDto, currentUserId);
                if (result) {
                    return Ok(new { Message = "诊断目录创建成功" });
                }
                return BadRequest("诊断目录创建失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "创建诊断目录失败");
                return StatusCode(500, "创建诊断目录失败");
            }
        }

        /// <summary>
        /// 更新诊断目录
        /// </summary>
        /// <param name="diagnosisCatalogDto">诊断目录对象</param>
        /// <returns>更新结果</returns>
        [HttpPut("diagnosis-catalogs")]
        [Authorize(Roles = "Admin,Doctor")]
        public async Task<ActionResult> UpdateDiagnosisCatalog([FromBody] DiagnosisCatalogDto diagnosisCatalogDto) {
            try {
                // TODO: 从当前用户上下文获取用户ID
                var currentUserId = Guid.NewGuid(); // 临时使用

                var result = await _configService.UpdateDiagnosisCatalogAsync(diagnosisCatalogDto, currentUserId);
                if (result) {
                    return Ok(new { Message = "诊断目录更新成功" });
                }
                return BadRequest("诊断目录更新失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "更新诊断目录失败");
                return StatusCode(500, "更新诊断目录失败");
            }
        }

        /// <summary>
        /// 删除诊断目录
        /// </summary>
        /// <param name="id">诊断目录ID</param>
        /// <returns>删除结果</returns>
        [HttpDelete("diagnosis-catalogs/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> DeleteDiagnosisCatalog(Guid id) {
            try {
                var result = await _configService.DeleteDiagnosisCatalogAsync(id);
                if (result) {
                    return Ok(new { Message = "诊断目录删除成功" });
                }
                return BadRequest("诊断目录删除失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "删除诊断目录失败: {Id}", id);
                return StatusCode(500, "删除诊断目录失败");
            }
        }

        // ==================== 治疗目录管理 ====================

        /// <summary>
        /// 获取所有治疗目录
        /// </summary>
        /// <returns>治疗目录列表</returns>
        [HttpGet("treatment-catalogs")]
        public async Task<ActionResult<List<TreatmentCatalogDto>>> GetTreatmentCatalogs() {
            try {
                var catalogs = await _configService.GetTreatmentCatalogsAsync();
                return Ok(catalogs);
            } catch (Exception ex) {
                _logger.LogError(ex, "获取治疗目录失败");
                return StatusCode(500, "获取治疗目录失败");
            }
        }

        // ==================== 缓存管理 ====================

        /// <summary>
        /// 刷新所有配置缓存
        /// </summary>
        /// <returns>刷新结果</returns>
        [HttpPost("cache/refresh-all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RefreshAllCache() {
            try {
                var result = await _configService.RefreshAllCacheAsync();
                if (result) {
                    return Ok(new { Message = "缓存刷新成功" });
                }
                return BadRequest("缓存刷新失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "刷新缓存失败");
                return StatusCode(500, "刷新缓存失败");
            }
        }

        /// <summary>
        /// 刷新设置缓存
        /// </summary>
        /// <returns>刷新结果</returns>
        [HttpPost("cache/refresh-settings")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> RefreshSettingCache() {
            try {
                var result = await _configService.RefreshSettingCacheAsync();
                if (result) {
                    return Ok(new { Message = "设置缓存刷新成功" });
                }
                return BadRequest("设置缓存刷新失败");
            } catch (Exception ex) {
                _logger.LogError(ex, "刷新设置缓存失败");
                return StatusCode(500, "刷新设置缓存失败");
            }
        }
    }

    /// <summary>
    /// 设置配置请求模型
    /// </summary>
    public class SetSettingRequest {
        public string Key { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Group { get; set; }
    }
}