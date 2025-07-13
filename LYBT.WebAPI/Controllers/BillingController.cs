using LYBT.Module.Billing.Dtos;
using LYBT.Module.Billing.Interfaces;
using LYBT.Common.Enums;
using Microsoft.AspNetCore.Mvc;

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

        [HttpPost("mark-paid/{id}")]
        public async Task<ActionResult> MarkAsPaid(Guid id) {
            var success = await _billingService.MarkAsPaidAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("complete/{id}")]
        public async Task<ActionResult> MarkAsCompleted(Guid id) {
            var success = await _billingService.MarkAsCompletedAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("request-refund/{id}")]
        public async Task<ActionResult> RequestRefund(Guid id, [FromBody] string reason) {
            var success = await _billingService.RequestRefundAsync(id, reason);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("approve-refund/{id}")]
        public async Task<ActionResult> ApproveRefund(Guid id) {
            var success = await _billingService.ApproveRefundAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("reject-refund/{id}")]
        public async Task<ActionResult> RejectRefund(Guid id) {
            var success = await _billingService.RejectRefundAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("cancel/{id}")]
        public async Task<ActionResult> Cancel(Guid id) {
            var success = await _billingService.CancelAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpGet("patient/{patientId}")]
        public async Task<ActionResult<List<BillingDto>>> GetByPatientId(Guid patientId) {
            var list = await _billingService.GetByPatientIdAsync(patientId);
            return Ok(list);
        }

        [HttpGet("search")]
        public async Task<ActionResult<List<BillingDto>>> Search(string keyword) {
            var list = await _billingService.SearchAsync(keyword);
            return Ok(list);
        }

        [HttpGet("refundable")]
        public async Task<ActionResult<List<BillingDto>>> GetRefundableBills() {
            var list = await _billingService.GetRefundableBillsAsync();
            return Ok(list);
        }

        [HttpGet("status/{status}")]
        public async Task<ActionResult<List<BillingDto>>> GetByStatus(BillingStatus status) {
            var list = await _billingService.GetByStatusAsync(status);
            return Ok(list);
        }
    }
}