using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LYBT.LocalWebAPI.Data;
using LYBT.Entities.Formulas;

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
    }
}
