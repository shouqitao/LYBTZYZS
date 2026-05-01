using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FormulasController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public FormulasController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/formulas?keyword=
        [HttpGet]
        public async Task<ActionResult<List<Formula>>> GetFormulas([FromQuery] string keyword)
        {
            var q = _db.Formulas.AsNoTracking().Where(f => !f.IsDeleted);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                q = q.Where(f => f.Name.Contains(keyword));
            }
            return await q.ToListAsync();
        }

        // GET /api/formulas/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Formula>> GetFormula(Guid id)
        {
            var formula = await _db.Formulas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (formula == null) return NotFound();
            return formula;
        }

        // POST /api/formulas
        [HttpPost]
        public async Task<IActionResult> CreateFormula([FromBody] Formula formula)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            _db.Formulas.Add(formula);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFormula), new { id = formula.Id }, formula);
        }

        // PUT /api/formulas/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFormula(Guid id, [FromBody] Formula updated)
        {
            if (id != updated.Id) return BadRequest("ID mismatch between URL and payload.");
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var existing = await _db.Formulas.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            _db.Entry(existing).CurrentValues.SetValues(updated);
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        // DELETE /api/formulas/{id} -> soft delete
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFormula(Guid id)
        {
            var existing = await _db.Formulas.FindAsync(id);
            if (existing == null || existing.IsDeleted) return NotFound();
            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/formulas/batch-delete
        [HttpPost("batch-delete")]
        public async Task<IActionResult> BatchDelete([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.IsDeleted = true;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/batch-enable
        [HttpPost("batch-enable")]
        public async Task<IActionResult> BatchEnable([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.Status = CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/batch-disable
        [HttpPost("batch-disable")]
        public async Task<IActionResult> BatchDisable([FromBody] List<Guid> ids)
        {
            if (ids == null || ids.Count == 0) return BadRequest("ids is required.");
            var formulas = await _db.Formulas.Where(f => ids.Contains(f.Id) && !f.IsDeleted).ToListAsync();
            foreach (var f in formulas) f.Status = CommonStatus.Disabled;
            await _db.SaveChangesAsync();
            return Ok(new { count = formulas.Count });
        }

        // POST /api/formulas/{id}/toggle-status
        [HttpPost("{id}/toggle-status")]
        public async Task<ActionResult<Formula>> ToggleStatus(Guid id)
        {
            var formula = await _db.Formulas.FindAsync(id);
            if (formula == null || formula.IsDeleted) return NotFound();
            formula.Status = formula.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
            await _db.SaveChangesAsync();
            return Ok(formula);
        }

        // POST /api/formulas/{id}/restore
        [HttpPost("{id}/restore")]
        public async Task<ActionResult<Formula>> Restore(Guid id)
        {
            var formula = await _db.Formulas.IgnoreQueryFilters().FirstOrDefaultAsync(f => f.Id == id);
            if (formula == null) return NotFound();
            if (!formula.IsDeleted) return BadRequest("Formula is not deleted.");
            formula.IsDeleted = false;
            await _db.SaveChangesAsync();
            return Ok(formula);
        }

        // POST /api/formulas/{id}/clone
        [HttpPost("{id}/clone")]
        public async Task<ActionResult<Formula>> Clone(Guid id)
        {
            var source = await _db.Formulas
                .AsNoTracking()
                .Include(f => f.Herbs)
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);
            if (source == null) return NotFound();

            var clone = new Formula
            {
                Id = Guid.NewGuid(),
                Name = source.Name,
                Effect = source.Effect,
                Indication = source.Indication,
                Usage = source.Usage,
                Remark = source.Remark,
                Property = source.Property,
                Status = source.Status,
                IsShared = source.IsShared,
                ValidationStatus = FormulaValidationStatus.Draft,
                Category = source.Category,
                FormulaType = source.FormulaType,
                UserId = source.UserId,
                IsDeleted = false,
                Herbs = source.Herbs?.Select(h => new FormulaHerbItem
                {
                    Id = Guid.NewGuid(),
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    Remark = h.Remark,
                    OriginalHerbName = h.OriginalHerbName,
                    IsValidated = h.IsValidated,
                    Usage = h.Usage,
                    ProcessingMethod = h.ProcessingMethod,
                    DecocteMethod = h.DecocteMethod
                }).ToList() ?? new List<FormulaHerbItem>()
            };

            _db.Formulas.Add(clone);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetFormula), new { id = clone.Id }, clone);
        }

        // GET /api/formulas/export
        [HttpGet("export")]
        public async Task<ActionResult<List<Formula>>> Export()
        {
            var formulas = await _db.Formulas
                .AsNoTracking()
                .Where(f => !f.IsDeleted)
                .Include(f => f.Herbs)
                .ToListAsync();
            return Ok(formulas);
        }

        // GET /api/formulas/import-template
        [HttpGet("import-template")]
        public IActionResult ExportTemplate()
        {
            var template = new[]
            {
                new { Name = "", Effect = "", Indication = "", Usage = "", Category = "", FormulaType = "", Remark = "" }
            };
            return Ok(template);
        }

        // POST /api/formulas/batch-import
        [HttpPost("batch-import")]
        public async Task<IActionResult> Import([FromBody] List<Formula> formulas)
        {
            if (formulas == null || formulas.Count == 0) return BadRequest("formulas is required.");
            int count = 0;
            foreach (var incoming in formulas)
            {
                var existing = await _db.Formulas.Include(f => f.Herbs).FirstOrDefaultAsync(f => f.Id == incoming.Id);
                if (existing != null)
                {
                    _db.Entry(existing).CurrentValues.SetValues(incoming);
                    if (incoming.Herbs != null)
                    {
                        existing.Herbs.Clear();
                        foreach (var herb in incoming.Herbs)
                            existing.Herbs.Add(herb);
                    }
                }
                else
                {
                    _db.Formulas.Add(incoming);
                }
                count++;
            }
            await _db.SaveChangesAsync();
            return Ok(new { count });
        }

        // GET /api/formulas/categories
        [HttpGet("categories")]
        public async Task<ActionResult<List<string>>> GetCategories()
        {
            var categories = await _db.Formulas
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.Category != null)
                .Select(f => f.Category!)
                .Distinct()
                .ToListAsync();
            return Ok(categories);
        }
    }
}
