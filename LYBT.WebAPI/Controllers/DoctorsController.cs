using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Models.Doctors;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    /// <summary>
    /// 医生管理接口
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase {
        private readonly IDoctorService _doctorService;
        private readonly IDoctorInfoRequestService _infoRequestService;
        public DoctorsController(IDoctorService doctorService, IDoctorInfoRequestService infoRequestService) {
            _doctorService = doctorService;
            _infoRequestService = infoRequestService;
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<DoctorDto>>> Search([FromQuery] string keyword) {
            var list = await _doctorService.SearchAsync(keyword);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id) {
            var item = await _doctorService.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] DoctorCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _doctorService.AddAsync(dto);
            return result ? Ok() : BadRequest();
        }

        [HttpPut("update")]
        public async Task<IActionResult> Update([FromBody] DoctorEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _doctorService.UpdateAsync(dto);
            return result ? Ok() : BadRequest();
        }

        [HttpPut("disable/{id}")]
        public async Task<IActionResult> Disable(Guid id) {
            var ok = await _doctorService.DisableAsync(id);
            return ok ? Ok() : NotFound();
        }

        [HttpPut("enable/{id}")]
        public async Task<IActionResult> Enable(Guid id) {
            var ok = await _doctorService.EnableAsync(id);
            return ok ? Ok() : NotFound();
        }

        [HttpPost("paged")]
        public async Task<ActionResult<PagedResultDto<DoctorDto>>> GetPaged([FromBody] DoctorQueryDto query) {
            var result = await _doctorService.GetPagedAsync(query);
            return Ok(result);
        }

        [HttpPut("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchIdsDto dto) {
            var count = await _doctorService.BatchDisableAsync(dto.Ids);
            return Ok(new { count });
        }

        [HttpPut("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchIdsDto dto) {
            var count = await _doctorService.BatchEnableAsync(dto.Ids);
            return Ok(new { count });
        }

        /// <summary>
        /// 重置医生密码，必须提供新密码
        /// </summary>
        [HttpPut("reset-password/{id}")]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] ResetPasswordDto dto) {
            var ok = await _doctorService.ResetPasswordAsync(id, dto.NewPassword);
            return ok ? Ok() : NotFound();
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto) {
            var ok = await _doctorService.ChangePasswordAsync(dto.DoctorId, dto.OldPassword, dto.NewPassword);
            return ok ? Ok() : BadRequest();
        }

        [HttpGet("roles")]
        public IActionResult GetRoles() {
            var roles = Enum.GetNames(typeof(LYBT.Common.Enums.Users.UserRole));
            return Ok(roles);
        }

        [HttpPost("InfoRequest")]
        public async Task<IActionResult> SubmitInfoRequest([FromBody] DoctorInfoRequestModel model) {
            var ok = await _infoRequestService.SubmitAsync(model);
            return ok ? Ok() : BadRequest();
        }

        [HttpGet("InfoRequest/pending")]
        public async Task<ActionResult<List<DoctorInfoRequestModel>>> GetPendingRequests() {
            var list = await _infoRequestService.GetPendingListAsync();
            return Ok(list);
        }

        [HttpPut("InfoRequest/{id}/approve")]
        public async Task<IActionResult> Approve(Guid id) {
            var ok = await _infoRequestService.ApproveAsync(id);
            return ok ? Ok() : NotFound();
        }

        [HttpPut("InfoRequest/{id}/reject")]
        public async Task<IActionResult> Reject(Guid id) {
            var ok = await _infoRequestService.RejectAsync(id);
            return ok ? Ok() : NotFound();
        }
    }
}