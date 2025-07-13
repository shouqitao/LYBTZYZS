using LYBT.Module.Queueing.Dtos;
using LYBT.Module.Queueing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.Module.Queueing.Controllers {

    /// <summary>
    /// 排队管理 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class QueueingController : ControllerBase {
        private readonly IQueueingService _queueingService;

        /// <summary>
        /// 构造方法，注入排队服务
        /// </summary>
        public QueueingController(IQueueingService queueingService) {
            _queueingService = queueingService;
        }

        /// <summary>
        /// 获取排队列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<QueueingDto>>> GetList() {
            var list = await _queueingService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取排队详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<QueueingDetailDto>> GetById(Guid id) {
            var detail = await _queueingService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增排队
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] QueueingCreateDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _queueingService.AddAsync(dto);
            if (!result)
                return BadRequest("新增排队失败");
            return Ok("新增排队成功");
        }

        /// <summary>
        /// 编辑排队
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] QueueingEditDto dto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            var result = await _queueingService.UpdateAsync(dto);
            if (!result)
                return BadRequest("编辑排队失败");
            return Ok("编辑排队成功");
        }

        /// <summary>
        /// 删除排队
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _queueingService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除排队成功");
        }

        /// <summary>
        /// 取消排队
        /// </summary>
        [HttpPost("cancel/{id}")]
        public async Task<ActionResult> Cancel(Guid id) {
            var result = await _queueingService.CancelAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpPost("complete/{id}")]
        public async Task<ActionResult> Complete(Guid id) {
            var result = await _queueingService.CompleteAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }

        [HttpPost("hold/{id}")]
        public async Task<ActionResult> Hold(Guid id) {
            var result = await _queueingService.HoldAsync(id);
            if (!result)
                return NotFound();
            return Ok();
        }
    }
}