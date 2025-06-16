using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Billing.Interfaces;
using LYBT.Module.Billing.Dtos;

namespace LYBT.Module.Billing.Controllers {
    /// <summary>
    /// 费用结算 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase {
        private readonly IBillingService _billingService;

        /// <summary>
        /// 构造方法，注入业务服务
        /// </summary>
        public BillingController(IBillingService billingService) {
            _billingService = billingService;
        }

        /// <summary>
        /// 获取费用结算列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<BillingDto>>> GetList() {
            var list = await _billingService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<BillingDetailDto>> GetById(Guid id) {
            var detail = await _billingService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增费用结算
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] BillingCreateDto billingCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _billingService.AddAsync(billingCreateDto);
            if (!result)
                return BadRequest("新增费用结算失败");

            return Ok("新增费用结算成功");
        }

        /// <summary>
        /// 编辑费用结算
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] BillingEditDto billingEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _billingService.UpdateAsync(billingEditDto);
            if (!result)
                return BadRequest("编辑费用结算失败");

            return Ok("编辑费用结算成功");
        }

        /// <summary>
        /// 删除费用结算
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _billingService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除费用结算成功");
        }
    }
}
