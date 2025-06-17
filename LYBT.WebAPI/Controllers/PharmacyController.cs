using LYBT.Module.Pharmacy.Dtos;
using LYBT.Module.Pharmacy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Pharmacy.Controllers {
    /// <summary>
    /// 药房 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PharmacyController : ControllerBase {
        private readonly IPharmacyService _pharmacyService;

        /// <summary>
        /// 构造方法，注入药房服务
        /// </summary>
        public PharmacyController(IPharmacyService pharmacyService) {
            _pharmacyService = pharmacyService;
        }

        /// <summary>
        /// 获取待抓药的处方列表
        /// </summary>
        [HttpGet("waiting")]
        public async Task<ActionResult<List<PharmacyDto>>> GetWaitingList() {
            var list = await _pharmacyService.GetWaitingListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取药房单列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<PharmacyDto>>> GetList() {
            var list = await _pharmacyService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<PharmacyDetailDto>> GetById(Guid id) {
            var detail = await _pharmacyService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增药房单
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] PharmacyCreateDto pharmacyCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _pharmacyService.AddAsync(pharmacyCreateDto);
            if (!result)
                return BadRequest("新增药房单失败");

            return Ok("新增药房单成功");
        }

        /// <summary>
        /// 编辑药房单
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] PharmacyEditDto pharmacyEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _pharmacyService.UpdateAsync(pharmacyEditDto);
            if (!result)
                return BadRequest("编辑药房单失败");

            return Ok("编辑药房单成功");
        }

        /// <summary>
        /// 删除药房单
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _pharmacyService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除药房单成功");
        }

        /// <summary>
        /// 标记处方为已抓药
        /// </summary>
        [HttpPost("{id}/prepared")]
        public async Task<ActionResult> MarkAsPrepared(Guid id) {
            var result = await _pharmacyService.MarkAsPreparedAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}
