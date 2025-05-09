using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.DTOs;
using SonicPoints.Models;
using System.Security.Claims;

namespace SonicPoints.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;

        public FeedbackController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid feedback data.");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var feedback = new Feedback
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Reason = dto.Reason,
                Type = dto.Type,
                Rating = dto.Rating,
                SubmittedAt = DateTime.UtcNow,
                SubmittedByUserId = userId
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Feedback submitted successfully." });
        }

        [HttpGet("project/{projectId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllFeedbackForProject(int projectId)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.Reason.Contains($"Project ID: {projectId}"))
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            return Ok(feedbacks);
        }
    }
}
