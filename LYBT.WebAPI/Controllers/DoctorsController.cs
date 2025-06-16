using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Doctors.Interfaces;
using LYBT.Module.Doctors.Dtos;

namespace LYBT.Module.Doctors.Controllers {
    /// <summary>
    /// 医生 API 控制器
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DoctorsController : ControllerBase {
        private readonly IDoctorService _doctorService;

        /// <summary>
        /// 构造方法，注入医生服务
        /// </summary>
        public DoctorsController(IDoctorService doctorService) {
            _doctorService = doctorService;
        }

        /// <summary>
        /// 获取医生列表
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<DoctorDto>>> GetList() {
            var list = await _doctorService.GetListAsync();
            return Ok(list);
        }

        /// <summary>
        /// 获取医生详情
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<DoctorDetailDto>> GetById(Guid id) {
            var detail = await _doctorService.GetByIdAsync(id);
            if (detail == null)
                return NotFound();
            return Ok(detail);
        }

        /// <summary>
        /// 新增医生
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> Add([FromBody] DoctorCreateDto doctorCreateDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _doctorService.AddAsync(doctorCreateDto);
            if (!result)
                return BadRequest("新增医生失败");

            return Ok("新增医生成功");
        }

        /// <summary>
        /// 编辑医生
        /// </summary>
        [HttpPut]
        public async Task<ActionResult> Update([FromBody] DoctorEditDto doctorEditDto) {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _doctorService.UpdateAsync(doctorEditDto);
            if (!result)
                return BadRequest("编辑医生失败");

            return Ok("编辑医生成功");
        }

        /// <summary>
        /// 删除医生
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(Guid id) {
            var result = await _doctorService.DeleteAsync(id);
            if (!result)
                return NotFound();
            return Ok("删除医生成功");
        }
    }
}
