using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Concert_Backend.Data;
using Concert_Backend.Models;
using System.Text;

namespace Concert_Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;
        public AdminController(AppDbContext context) { _context = context; }

        // --- 0. DASHBOARD STATS (With Backend Pagination) ---
        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] int page = 1)
        {
            try
            {
                const int pageSize = 5;
                var totalRevenue = await _context.Tickets.SumAsync(t => (double?)t.Price) ?? 0;
                var totalTickets = await _context.Purchases.SumAsync(p => (int?)p.Quantity) ?? 0;
                var totalPurchasesCount = await _context.Purchases.CountAsync();

                var recentPurchases = await _context.Purchases
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new {
                        p.PaymentId, p.UserEmail, p.TicketType, p.Quantity, p.CreatedAt,
                        ConcertTitle = _context.Tickets
                            .Where(t => t.PaymentId == p.PaymentId)
                            .Select(t => t.Concert.ConcertTitle)
                            .FirstOrDefault() ?? "Unknown Event"
                    }).ToListAsync();

                return Ok(new { 
                    totalRevenue, totalTickets, recentPurchases,
                    totalPages = (int)Math.Ceiling((double)totalPurchasesCount / pageSize),
                    currentPage = page
                });
            }
            catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
        }

        // --- 1. USER MANAGEMENT LOGIC ---
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers() => Ok(await _context.Users.Select(u => new { u.Name, u.Email, u.Role, u.IsSuspended }).ToListAsync());

        [HttpPut("toggle-suspension/{email}")]
        public async Task<IActionResult> ToggleSuspension(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null) return NotFound();
            user.IsSuspended = !user.IsSuspended;
            await _context.SaveChangesAsync();
            return Ok(new { isSuspended = user.IsSuspended });
        }

        [HttpPut("update-role")]
        public async Task<IActionResult> UpdateRole([FromBody] RoleUpdateDto data)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == data.Email);
            if (user == null) return NotFound();
            user.Role = data.Role;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Role updated" });
        }

        // --- 2. CONCERT CRUD ---
        [HttpPost("add-concert")]
        public async Task<IActionResult> AddConcert([FromBody] Concert concert)
        {
            concert.ConcertId = 0;
            _context.Concerts.Add(concert);
            await _context.SaveChangesAsync();
            return Ok(concert);
        }

        [HttpPut("update-concert/{id}")]
        public async Task<IActionResult> UpdateConcert(int id, [FromBody] Concert updated)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();
            _context.Entry(concert).CurrentValues.SetValues(updated); // Syncs all fields automatically
            await _context.SaveChangesAsync();
            return Ok(concert);
        }

        [HttpDelete("delete-concert/{id}")]
        public async Task<IActionResult> DeleteConcert(int id)
        {
            var concert = await _context.Concerts.FindAsync(id);
            if (concert == null) return NotFound();
            _context.Concerts.Remove(concert);
            await _context.SaveChangesAsync();
            return Ok();
        }
    }
    public class RoleUpdateDto { public string Email { get; set; } public string Role { get; set; } }
}