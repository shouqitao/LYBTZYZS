using LYBT.Module.Settings.Interfaces;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers {

    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示EnumMappingsController。
/// </summary>
public class EnumMappingsController : ControllerBase {
        private readonly IEnumMappingsService _service;

        public EnumMappingsController(IEnumMappingsService service) {
            _service = service;
        }

        [HttpGet]
/// <summary>
/// 执行GetAllEnumMappings操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<Dictionary<string, Dictionary<int, string>>>> GetAllEnumMappings() {
            var mappings = await _service.GetAllAsync();
            return Ok(mappings);
        }
    }
}
