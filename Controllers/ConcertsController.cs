using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConcertsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ConcertsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Concerts
        [HttpGet]
        public async Task<IActionResult> GetConcerts()
        {
            try
            {
                var concerts = await _context.Concerts
                    .OrderByDescending(c => c.Date) // Show newest/upcoming first
                    .ToListAsync();

                return Ok(concerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving concerts", error = ex.Message });
            }
        }
    }
}