using LYBT.Common.Models;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 医生管理接口
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示DoctorsController。
/// </summary>
public class DoctorsController : ControllerBase {
        private readonly IDoctorService _doctorService;
        public DoctorsController(IDoctorService doctorService) {
            _doctorService = doctorService;
        }

        [HttpGet("search")]
/// <summary>
/// 执行Search操作。
/// </summary>
/// <param name="""">参数""</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<DoctorDto>>> Search([FromQuery] string keyword = "") {
            try {
                var list = await _doctorService.SearchAsync(keyword ?? "");
                return Ok(list);
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"搜索医生失败: {ex.Message}", details = ex.ToString() });
            }
        }

        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id) {
            try {
                var item = await _doctorService.GetByIdAsync(id);
                return item == null ? NotFound() : Ok(item);
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"获取医生详情失败: {ex.Message}" });
            }
        }

        [HttpGet("by-user/{userId}")]
/// <summary>
/// 执行GetByUserId操作。
/// </summary>
/// <param name="userId">参数userId</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<DoctorDetailDto>> GetByUserId(Guid userId) {
            try {
                var item = await _doctorService.GetByUserIdAsync(userId);
                return item == null ? NotFound() : Ok(item);
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"根据用户ID获取医生失败: {ex.Message}" });
            }
        }

        [HttpPost("add")]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<ApiSuccessResponse>> Add([FromBody] DoctorDetailDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(new ApiSuccessResponse { Success = false, Count = 0, Message = "参数验证失败" });
            try {
                var result = await _doctorService.AddAsync(dto);
                if (result)
                    return Ok(new ApiSuccessResponse { Success = true, Message = "新增成功" });
                return BadRequest(new ApiSuccessResponse { Success = false, Message = "新增失败" });
            } catch (Exception ex) {
                return BadRequest(new ApiSuccessResponse { Success = false, Count = 0, Message = ex.Message });
            }
        }

        [HttpPut("update")]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<ApiSuccessResponse>> Update([FromBody] DoctorDetailDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(new ApiSuccessResponse { Success = false, Count = 0, Message = "参数验证失败" });
            try {
                var result = await _doctorService.UpdateAsync(dto);
                if (result)
                    return Ok(new ApiSuccessResponse { Success = true, Message = "保存成功" });
                return BadRequest(new ApiSuccessResponse { Success = false, Message = "保存失败" });
            } catch (Exception ex) {
                return BadRequest(new ApiSuccessResponse { Success = false, Count = 0, Message = ex.Message });
            }
        }

        [HttpPut("disable/{id}")]
/// <summary>
/// 执行Disable操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<IActionResult> Disable(Guid id) {
            try {
                var ok = await _doctorService.DisableAsync(id);
                return ok ? Ok(new ApiSuccessResponse { Success = true, Message = "禁用成功" }) : NotFound();
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"禁用医生失败: {ex.Message}" });
            }
        }

        [HttpPut("enable/{id}")]
/// <summary>
/// 执行Enable操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<IActionResult> Enable(Guid id) {
            try {
                var ok = await _doctorService.EnableAsync(id);
                return ok ? Ok(new ApiSuccessResponse { Success = true, Message = "启用成功" }) : NotFound();
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"启用医生失败: {ex.Message}" });
            }
        }

        [HttpPost("paged")]
/// <summary>
/// 执行GetPaged操作。
/// </summary>
/// <param name="query">参数query</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<PagedResultDto<DoctorDto>>> GetPaged([FromBody] DoctorQueryDto query) {
            try {
                var result = await _doctorService.GetPagedAsync(query);
                return Ok(result);
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"分页查询医生失败: {ex.Message}" });
            }
        }

        [HttpPut("batch-disable")]
/// <summary>
/// 执行BatchDisable操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
            try {
                var count = await _doctorService.BatchDisableAsync(dto.Ids);
                return Ok(new ApiSuccessResponse { Success = true, Count = count, Message = $"成功禁用 {count} 个医生" });
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"批量禁用医生失败: {ex.Message}" });
            }
        }

        [HttpPut("batch-enable")]
/// <summary>
/// 执行BatchEnable操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
            try {
                var count = await _doctorService.BatchEnableAsync(dto.Ids);
                return Ok(new ApiSuccessResponse { Success = true, Count = count, Message = $"成功启用 {count} 个医生" });
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"批量启用医生失败: {ex.Message}" });
            }
        }

        [HttpGet("roles")]
/// <summary>
/// 执行GetRoles操作。
/// </summary>
/// <returns>返回值</returns>
        public IActionResult GetRoles() {
            try {
                var roles = Enum.GetNames(typeof(LYBT.Common.Enums.Users.UserRole));
                return Ok(roles);
            } catch (Exception ex) {
                return StatusCode(500, new { message = $"获取角色列表失败: {ex.Message}" });
            }
        }
    }
}
