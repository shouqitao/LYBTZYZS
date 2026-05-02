using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Herbs;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class HerbsController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public HerbsController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/herbs?keyword=&category=
        [HttpGet]
        public async Task<ActionResult<List<Herb>>> GetHerbs([FromQuery] string keyword, [FromQuery] string? category = null)
        {
            var q = _db.Herbs.AsNoTracking().Where(h => !h.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(h => h.Name.Contains(keyword) || (h.PinYinCode != null && h.PinYinCode.Contains(keyword)));
            }
            if (!string.IsNullOrWhiteSpace(category))
            {
                q = q.Where(h => h.Category == category);
            }
            return await q.ToListAsync();
        }

        // GET /api/herbs/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Herb>> GetHerb(Guid id)
        {
            var herb = await _db.Herbs.AsNoTracking().FirstOrDefaultAsync(h => h.Id == id && !h.IsDeleted);
            if (herb == null) return NotFound();
            return herb;
        }

        // POST /api/herbs
        [HttpPost]
        public async Task<IActionResult> CreateHerb([FromBody] Herb herb)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Herbs.Add(herb);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetHerb), new { id = herb.Id }, herb);
        }

        // PUT /api/herbs/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateHerb(Guid id, [FromBody] Herb updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Herbs.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/herbs/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteHerb(Guid id)
        {
            var existing = await _db.Herbs.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/herbs/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids 不能为空");

            var isReferenced = await _db.PrescriptionItems.AnyAsync(pi => request.Ids.Contains(pi.HerbId));
            if (isReferenced)
                return Conflict("部分药材被处方引用，无法删除");

            var herbs = await _db.Herbs.Where(h => request.Ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/batch-enable
        [HttpPost("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids 不能为空");

            var herbs = await _db.Herbs.Where(h => request.Ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.Status = CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/batch-disable
        [HttpPost("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] BatchDeleteInputDto request)
        {
            if (request?.Ids == null || request.Ids.Count == 0) return BadRequest("ids 不能为空");

            var herbs = await _db.Herbs.Where(h => request.Ids.Contains(h.Id) && !h.IsDeleted).ToListAsync();
            foreach (var h in herbs) h.Status = CommonStatus.Disabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = herbs.Count });
        }

        // POST /api/herbs/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<IActionResult> ToggleStatus(Guid id)
        {
            var herb = await _db.Herbs.FindAsync(id);
            if (herb == null || herb.IsDeleted) return NotFound();
            herb.Status = herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(herb);
        }

        // POST /api/herbs/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<IActionResult> Restore(Guid id)
        {
            var herb = await _db.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == id);
            if (herb == null) return NotFound();
            herb.IsDeleted = false;
            await _db.SaveChangesAsync();
            return Ok(herb);
        }

        // GET /api/herbs/export
        [HttpGet("export")]
        public async Task<ActionResult<List<Herb>>> Export()
        {
            return await _db.Herbs.AsNoTracking().Where(h => !h.IsDeleted).ToListAsync();
        }

        // GET /api/herbs/import-template
        [HttpGet("import-template")]
        public IActionResult ExportTemplate()
        {
            var template = new[]
            {
                new { Name = "", PinYinCode = "", Category = "", Unit = "", Property = "", Effect = "" }
            };
            return Ok(template);
        }

        // POST /api/herbs/batch-import
        [HttpPost("batch-import")]
        public async Task<ActionResult<HerbBatchImportResultDto>> Import([FromBody] HerbBatchImportInputDto request)
        {
            if (request == null || request.Herbs == null || request.Herbs.Count == 0)
                return BadRequest("导入列表不能为空");

            var result = new HerbBatchImportResultDto { ImportTime = DateTime.UtcNow, TotalCount = request.Herbs.Count };

            foreach (var dto in request.Herbs)
            {
                try
                {
                    var entity = new Herb
                    {
                        Id = dto.Id ?? Guid.NewGuid(),
                        Name = dto.Name,
                        PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode) ? dto.Name : dto.PinYinCode,
                        Category = dto.Category,
                        Properties = dto.Properties,
                        Origin = dto.Origin,
                        Spec = dto.Spec,
                        Unit = dto.Unit,
                        Price = dto.Price,
                        Effect = dto.Effect,
                        Usage = dto.Usage,
                        Remark = dto.Remark,
                        Status = CommonStatus.Enabled
                    };

                    if (dto.Id.HasValue)
                    {
                        var existing = await _db.Herbs.IgnoreQueryFilters().FirstOrDefaultAsync(h => h.Id == dto.Id.Value);
                        if (existing != null)
                        {
                            _db.Entry(existing).CurrentValues.SetValues(entity);
                            result.SuccessCount++;
                            continue;
                        }
                    }

                    _db.Herbs.Add(entity);
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailureCount++;
                    result.Failures.Add(new HerbImportFailureDto
                    {
                        HerbName = dto.Name,
                        Reason = "导入失败",
                        ErrorDetails = new List<string> { ex.Message }
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(result);
        }

        // GET /api/herbs/categories
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            return await _db.Herbs.AsNoTracking()
                .Where(h => !h.IsDeleted && h.Category != null)
                .Select(h => h.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();
        }
    }
}
