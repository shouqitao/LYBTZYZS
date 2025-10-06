using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 药材管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    [Authorize]
    public class HerbsController : BaseApiController
    {
        private readonly IHerbService _herbService;

        public HerbsController(
            IHerbService herbService,
            ILogger<HerbsController> logger,
            IMemoryCache cache) : base(logger, cache)
        {
            _herbService = herbService;
        }

        /// <summary>
        /// 获取药材分页列表
        /// </summary>
        [HttpGet]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        [OutputCache(PolicyName = "HerbsCache")]
        public async Task<ActionResult<ApiResponse<PagedResult<HerbDto>>>> GetList(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? keyword = null)
        {
            try
            {
                if (page <= 0 || pageSize <= 0 || pageSize > 100)
                {
                    return ValidationFailPaged<HerbDto>("页码和页大小参数无效（页码>0，页大小1-100）");
                }

                var result = await _herbService.GetPagedAsync(page, pageSize, keyword);
                return HandlePagedServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleExceptionPaged<HerbDto>(ex, "获取药材列表", new { page, pageSize, keyword });
            }
        }

        /// <summary>
        /// 根据ID获取药材详情
        /// </summary>
        [HttpGet("{id}")]
        [ResponseCache(Duration = 600, VaryByQueryKeys = new[] { "id" })]
        public async Task<ActionResult<ApiResponse<HerbDto>>> GetById(Guid id)
        {
            try
            {
                var validation = ValidateGuid<HerbDto>(id, "药材ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.GetByIdAsync(id);
                return HandleServiceResult(result, "查询成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "获取药材详情", id);
            }
        }

        /// <summary>
        /// 创建新药材
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ApiResponse<HerbDto>>> Create([FromBody] HerbCreateDto dto)
        {
            try
            {
                var validation = ValidateModel<HerbDto>();
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.CreateAsync(dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("创建药材", result.Data, result.Data.Id);
                }

                return HandleServiceResult(result, "药材创建成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "创建药材", dto);
            }
        }

        /// <summary>
        /// 更新药材信息
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ApiResponse<HerbDto>>> Update(Guid id, [FromBody] HerbUpdateDto dto)
        {
            try
            {
                var idValidation = ValidateGuid<HerbDto>(id, "药材ID");
                if (idValidation != null)
                {
                    return idValidation;
                }

                var modelValidation = ValidateModel<HerbDto>();
                if (modelValidation != null)
                {
                    return modelValidation;
                }

                // 确保使用路由中的ID
                dto.Id = id;

                var result = await _herbService.UpdateAsync(id, dto);
                if (result.IsSuccess && result.Data != null)
                {
                    LogOperation("更新药材信息", result.Data, id);
                }

                return HandleServiceResult(result, "药材信息更新成功");
            }
            catch (Exception ex)
            {
                return HandleException<HerbDto>(ex, "更新药材信息", new { id, dto });
            }
        }

        /// <summary>
        /// 删除药材（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult<ApiResponse>> Delete(Guid id)
        {
            try
            {
                var validation = ValidateGuid(id, "药材ID");
                if (validation != null)
                {
                    return validation;
                }

                var result = await _herbService.DeleteAsync(id);
                return HandleServiceResult(result, "删除成功");
            }
            catch (Exception ex)
            {
                return HandleException(ex, "删除药材", id);
            }
        }
    }
}
