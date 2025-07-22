using LYBT.Module.Registration.Dtos;
using LYBT.Common.Responses;
using LYBT.Module.Registration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LYBT.Module.Registration.Controllers {

    /// <summary>
    /// 挂号管理 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示RegistrationController。
/// </summary>
    public class RegistrationController : ControllerBase {
        private readonly IRegistrationService _registrationService;

        /// <summary>
        /// 构造方法，注入挂号服务
        /// </summary>
        public RegistrationController(IRegistrationService registrationService) {
            _registrationService = registrationService;
        }

        /// <summary>
        /// 获取挂号列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<RegistrationDto>>> GetList() {
            var list = await _registrationService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<RegistrationDetailDto>> GetById(Guid id) {
            var detail = await _registrationService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增挂号
        /// </summary>
        [HttpPost]
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Add([FromBody] RegistrationCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _registrationService.AddAsync(dto);
            if (!result)
                return BadRequest("新增挂号失败");
            return Ok("新增挂号成功");
        }

        /// <summary>
        /// 编辑挂号
        /// </summary>
        [HttpPut]
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Update([FromBody] RegistrationEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _registrationService.UpdateAsync(dto);
            if (!result)
                return BadRequest("编辑挂号失败");
            return Ok("编辑挂号成功");
        }

        /// <summary>
        /// 删除挂号
        /// </summary>
        [HttpDelete("{id}")]
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _registrationService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除挂号成功");
        }

        /// <summary>
        /// 取消挂号（软删除）
        /// </summary>
        [HttpPost("cancel/{id}")]
/// <summary>
/// 执行Cancel操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Cancel(Guid id) {
            var result = await _registrationService.CancelAsync(id);
            if (!result)
                return NotFound();
            return Ok("取消挂号成功");
        }
    }
}
