using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LYBT.LocalWebAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace LYBT.LocalWebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class HealthController : ControllerBase
    {
        private readonly LocalWebApiDbContext _db;

        public HealthController(LocalWebApiDbContext db)
        {
            _db = db;
        }

        // GET /api/health
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            bool canConnect = false;
            try
            {
                canConnect = await _db.Database.CanConnectAsync();
            }
            catch
            {
                canConnect = false;
            }

            var status = canConnect ? "Healthy" : "Degraded";
            var result = new
            {
                status = status,
                timestamp = DateTime.UtcNow,
                database = canConnect ? "Connected" : "Disconnected"
            };
            return Ok(result);
        }
    }
}
