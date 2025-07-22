using LYBT.Module.Pharmacy.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Pharmacy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Pharmacy.Controllers {

    /// <summary>
    /// 药房 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示PharmacyController。
/// </summary>
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
/// <summary>
/// 执行GetWaitingList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<PharmacyDto>>> GetWaitingList() {
            var list = await _pharmacyService.GetWaitingListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取药房单列表
        /// </summary>
        [HttpGet]
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<PharmacyDto>>> GetList() {
            var list = await _pharmacyService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取药房单详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="pharmacyCreateDto">参数pharmacyCreateDto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="pharmacyEditDto">参数pharmacyEditDto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行MarkAsPrepared操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> MarkAsPrepared(Guid id) {
            var result = await _pharmacyService.MarkAsPreparedAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}
