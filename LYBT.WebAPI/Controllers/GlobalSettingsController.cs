using LYBT.Module.Settings.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Settings.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示GlobalSettingsController。
/// </summary>
public class GlobalSettingsController : ControllerBase {
        private readonly IGlobalSettingsService _service;

        public GlobalSettingsController(IGlobalSettingsService service) {
            _service = service;
        }

        [HttpGet]
/// <summary>
/// 执行GetSettings操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<GlobalSettingsDto?>> GetSettings() {
            var settings = await _service.GetAsync();
            return Ok(settings);
        }

        [HttpPut]
/// <summary>
/// 执行UpdateSettings操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> UpdateSettings([FromBody] GlobalSettingsDto dto) {
            var result = await _service.SaveAsync(dto);
            if (!result)
                return BadRequest();
            return Ok();
        }
    }
}
