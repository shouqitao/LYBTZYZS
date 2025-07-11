using LYBT.Module.Registration.Dtos;
using LYBT.Module.Registration.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LYBT.Module.Registration.Controllers {

    /// <summary>
    /// 挂号管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
        public async Task<ActionResult<List<RegistrationDto>>> GetList() {
            var list = await _registrationService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取挂号详情
        /// </summary>
        [HttpGet("{id}")]
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
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _registrationService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除挂号成功");
        }
    }
}