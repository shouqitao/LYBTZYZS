using LYBT.Module.Billing.Dtos;
using LYBT.Common.Responses;
using Microsoft.AspNetCore.Authorization;
using LYBT.Module.Billing.Interfaces;
using LYBT.Common.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Billing.Controllers {

    /// <summary>
    /// 费用结算 API 控制器
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
/// <summary>
/// 表示BillingController。
/// </summary>
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
/// <summary>
/// 执行GetList操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<BillingDto>>> GetList() {
            var list = await _billingService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取费用结算详情
        /// </summary>
        [HttpGet("{id}")]
/// <summary>
/// 执行GetById操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Add操作。
/// </summary>
/// <param name="billingCreateDto">参数billingCreateDto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Update操作。
/// </summary>
/// <param name="billingEditDto">参数billingEditDto</param>
/// <returns>返回值</returns>
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
/// <summary>
/// 执行Delete操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _billingService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除费用结算成功");
        }

        [HttpPost("mark-paid/{id}")]
/// <summary>
/// 执行MarkAsPaid操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> MarkAsPaid(Guid id) {
            var success = await _billingService.MarkAsPaidAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("complete/{id}")]
/// <summary>
/// 执行MarkAsCompleted操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> MarkAsCompleted(Guid id) {
            var success = await _billingService.MarkAsCompletedAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("request-refund/{id}")]
/// <summary>
/// 执行RequestRefund操作。
/// </summary>
/// <param name="id">参数id</param>
/// <param name="reason">参数reason</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> RequestRefund(Guid id, [FromBody] string reason) {
            var success = await _billingService.RequestRefundAsync(id, reason);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("approve-refund/{id}")]
/// <summary>
/// 执行ApproveRefund操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> ApproveRefund(Guid id) {
            var success = await _billingService.ApproveRefundAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("reject-refund/{id}")]
/// <summary>
/// 执行RejectRefund操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> RejectRefund(Guid id) {
            var success = await _billingService.RejectRefundAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpPost("cancel/{id}")]
/// <summary>
/// 执行Cancel操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<ActionResult> Cancel(Guid id) {
            var success = await _billingService.CancelAsync(id);
            if (!success)
                return NotFound();
            return Ok();
        }

        [HttpGet("patient/{patientId}")]
/// <summary>
/// 执行GetByPatientId操作。
/// </summary>
/// <param name="patientId">参数patientId</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<BillingDto>>> GetByPatientId(Guid patientId) {
            var list = await _billingService.GetByPatientIdAsync(patientId);
            return Ok(list);
        }

        [HttpGet("search")]
/// <summary>
/// 执行Search操作。
/// </summary>
/// <param name="keyword">参数keyword</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<BillingDto>>> Search(string keyword) {
            var list = await _billingService.SearchAsync(keyword);
            return Ok(list);
        }

        [HttpGet("refundable")]
/// <summary>
/// 执行GetRefundableBills操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<BillingDto>>> GetRefundableBills() {
            var list = await _billingService.GetRefundableBillsAsync();
            return Ok(list);
        }

        [HttpGet("status/{status}")]
/// <summary>
/// 执行GetByStatus操作。
/// </summary>
/// <param name="status">参数status</param>
/// <returns>返回值</returns>
        public async Task<ActionResult<List<BillingDto>>> GetByStatus(BillingStatus status) {
            var list = await _billingService.GetByStatusAsync(status);
            return Ok(list);
        }
    }
}
