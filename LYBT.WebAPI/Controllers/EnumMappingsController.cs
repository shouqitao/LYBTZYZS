using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Settings.Interfaces;

namespace LYBT.WebAPI.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class EnumMappingsController : ControllerBase {
        private readonly IEnumMappingsService _service;
        public EnumMappingsController(IEnumMappingsService service) {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<Dictionary<string, Dictionary<int, string>>>> GetAllEnumMappings() {
            var mappings = await _service.GetAllAsync();
            return Ok(mappings);
        }
    }
}
