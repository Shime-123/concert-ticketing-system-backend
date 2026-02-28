using Microsoft.AspNetCore.Mvc;
using Concert_Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Concert_Backend.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class TestDbController : ControllerBase
	{
		private readonly AppDbContext _context;

		public TestDbController(AppDbContext context)
		{
			_context = context;
		}

		[HttpGet("check")]
		public async Task<IActionResult> CheckConnection()
		{
			try
			{
				// This will try to count users in your SSMS table
				var userCount = await _context.Users.CountAsync();
				return Ok(new { Message = "Connected to SSMS!", TotalUsers = userCount });
			}
			catch (Exception ex)
			{
				return BadRequest(new { Message = "Connection failed", Error = ex.Message });
			}
		}
	}
}