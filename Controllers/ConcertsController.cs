using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;
using Concert_Backend.Models;

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
                    .OrderByDescending(c => c.Date)
                    .ToListAsync();

                return Ok(concerts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error retrieving concerts", error = ex.Message });
            }
        }

        // POST: api/Concerts
        [HttpPost]
        public async Task<IActionResult> CreateConcert([FromBody] Concert concert)
        {
            try
            {
                if (concert == null) return BadRequest("Concert data is null");

                // --- PATH CLEANING LOGIC ---
                if (!string.IsNullOrEmpty(concert.ImageUrl))
                {
                    // If it's a web URL, leave it alone. 
                    // If it's a file path, extract just the filename.
                    if (!concert.ImageUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        // Path.GetFileName handles both \ and / separators
                        string fileName = Path.GetFileName(concert.ImageUrl);
                        
                        // Standardize the path to 'assets/filename.jpg'
                        concert.ImageUrl = $"assets/{fileName}";
                        
                        Console.WriteLine($"🧹 Cleaned Image Path to: {concert.ImageUrl}");
                    }
                }

                _context.Concerts.Add(concert);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Concert created successfully", concert });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating concert", error = ex.Message });
            }
        }
    }
}