using Asp.Versioning;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Doctors;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 医生管理接口（简化版）
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class DoctorsController : BaseController {
        private readonly IDoctorService _doctorService;
        
        public DoctorsController(
            IDoctorService doctorService,
            IMemoryCache cache,
            ILogger<DoctorsController> logger) 
            : base(logger, cache) {
            _doctorService = doctorService;
        }

        /// <summary>
        /// 获取所有医生列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DoctorDto>>> GetAll() {
            var (_, _, operatorRole) = GetOperator();
            var doctors = await _doctorService.GetAllAsync(operatorRole);
            return Ok(doctors);
        }

        /// <summary>
        /// 分页查询医生列表
        /// </summary>
        [HttpPost("paged")]
        public async Task<ActionResult<PaginatedResult<DoctorDto>>> GetPaged([FromBody] DoctorQueryDto query) {
            var (_, _, operatorRole) = GetOperator();
            var result = await _doctorService.GetPagedAsync(query, operatorRole);
            return Ok(result);
        }

        /// <summary>
        /// 根据ID获取医生详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id) {
            var (_, _, operatorRole) = GetOperator();
            var doctor = await _doctorService.GetByIdAsync(id, operatorRole);
            
            if (doctor == null) {
                return NotFound(new ProblemDetails {
                    Title = "医生不存在",
                    Detail = $"未找到ID为 {id} 的医生",
                    Status = 404
                });
            }
            
            return Ok(doctor);
        }

        /// <summary>
        /// 搜索医生
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<List<DoctorDto>>> Search([FromQuery] string keyword = "") {
            var (_, _, operatorRole) = GetOperator();
            var doctors = await _doctorService.SearchAsync(keyword, operatorRole);
            return Ok(doctors);
        }

        /// <summary>
        /// 获取可用医生列表（用于挂号选择）
        /// </summary>
        [HttpGet("available")]
        public async Task<ActionResult<List<DoctorDto>>> GetAvailable() {
            var doctors = await _doctorService.GetAvailableDoctorsAsync();
            return Ok(doctors);
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDetailDto>> Create([FromBody] DoctorCreateDto dto) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var (operatorId, operatorName, _) = GetOperator();
            var doctor = await _doctorService.CreateAsync(dto, operatorId, operatorName);
            
            if (doctor == null) {
                return BadRequest(new ProblemDetails {
                    Title = "创建失败",
                    Detail = "创建医生失败",
                    Status = 400
                });
            }
            
            return Ok(doctor);
        }

        /// <summary>
        /// 更新医生信息
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<DoctorDetailDto>> Update(Guid id, [FromBody] DoctorUpdateDto dto) {
            if (!ModelState.IsValid) {
                return BadRequest(ModelState);
            }

            var (operatorId, operatorName, _) = GetOperator();
            var doctor = await _doctorService.UpdateAsync(id, dto, operatorId, operatorName);
            
            if (doctor == null) {
                return NotFound(new ProblemDetails {
                    Title = "医生不存在",
                    Detail = $"未找到ID为 {id} 的医生",
                    Status = 404
                });
            }
            
            return Ok(doctor);
        }

        /// <summary>
        /// 删除医生（软删除）
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> Delete(Guid id) {
            var (operatorId, operatorName, _) = GetOperator();
            var success = await _doctorService.DeleteAsync(id, operatorId, operatorName);
            
            if (!success) {
                return NotFound(new ProblemDetails {
                    Title = "医生不存在",
                    Detail = $"未找到ID为 {id} 的医生",
                    Status = 404
                });
            }
            
            return Ok(new { message = "删除成功" });
        }

        /// <summary>
        /// 设置医生状态
        /// </summary>
        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SetStatus(Guid id, [FromBody] DoctorStatus status) {
            var (operatorId, operatorName, _) = GetOperator();
            var success = await _doctorService.SetStatusAsync(id, status, operatorId, operatorName);
            
            if (!success) {
                return NotFound(new ProblemDetails {
                    Title = "医生不存在",
                    Detail = $"未找到ID为 {id} 的医生",
                    Status = 404
                });
            }
            
            return Ok(new { message = "状态更新成功" });
        }

        /// <summary>
        /// 根据用户ID获取医生信息
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<DoctorDetailDto>> GetByUserId(Guid userId) {
            var (_, _, operatorRole) = GetOperator();
            var doctor = await _doctorService.GetByUserIdAsync(userId, operatorRole);
            
            if (doctor == null) {
                return NotFound(new ProblemDetails {
                    Title = "医生不存在",
                    Detail = $"用户 {userId} 不是医生",
                    Status = 404
                });
            }
            
            return Ok(doctor);
        }
    }
}