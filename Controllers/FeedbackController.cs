using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SonicPoints.Data;
using SonicPoints.DTOs;
using SonicPoints.Models;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;

namespace SonicPoints.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    [Authorize]
    public class FeedbackController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public FeedbackController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        [HttpPost("submit/{projectId}")]
        public async Task<IActionResult> SubmitProjectFeedback(int projectId, [FromBody] FeedbackDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null) return NotFound("❌ Project not found.");

            // Save feedback
            var feedback = new Feedback
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Type = dto.Type,
                Rating = dto.Rating,
                
                Reason = $"Project ID: {projectId} - Deleted/Expired",
                SubmittedAt = DateTime.UtcNow,
                SubmittedByUserId = userId
            };

            _context.Feedbacks.Add(feedback);

            // Email notifications
            await NotifySuperadminAsync($"📩 Feedback submitted for Project ID: {projectId}.\n\nReason: Deleted/Expired.");
            await NotifyProjectAdminsAsync(projectId);

            // Delete related data
            var relatedTasks = _context.Tasks.Where(t => t.ProjectId == projectId);
            _context.Tasks.RemoveRange(relatedTasks);

            var projectUsers = _context.ProjectUsers.Where(pu => pu.ProjectId == projectId);
            _context.ProjectUsers.RemoveRange(projectUsers);

            var leaderboardEntries = _context.Leaderboards.Where(l => l.ProjectId == projectId);
            _context.Leaderboards.RemoveRange(leaderboardEntries);

            var redeemHistory = _context.RedeemHistory.Where(r => r.ProjectId == projectId);
            _context.RedeemHistory.RemoveRange(redeemHistory);

            var redeemableItems = _context.RedeemableItems.Where(i => i.ProjectId == projectId);
            _context.RedeemableItems.RemoveRange(redeemableItems);

            _context.Projects.Remove(project);

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Feedback submitted and project deleted." });
        }


        [HttpGet]

        public async Task<IActionResult> GetAllFeedback()
        {
            var feedbacks = await _context.Feedbacks.OrderByDescending(f => f.SubmittedAt).ToListAsync();
            return Ok(feedbacks);
        }

        [HttpGet("project/{projectId}")]

        public async Task<IActionResult> GetAllFeedbackForProject(int projectId)
        {
            var feedbacks = await _context.Feedbacks
                .Where(f => f.Reason.Contains($"Project ID: {projectId}"))
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            return Ok(feedbacks);
        }

        private async Task NotifySuperadminAsync(string body)
        {
            string superadminEmail = "sakchyamthapa4@gmail.com";
            await SendEmailAsync(superadminEmail, "🚨 Feedback Alert", body);
        }

        private async Task NotifyProjectAdminsAsync(int projectId)
        {
            var adminEmails = await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId && pu.Role == "Admin")
                .Include(pu => pu.User)
                .Select(pu => pu.User.Email)
                .Distinct()
                .ToListAsync();

            foreach (var email in adminEmails)
            {
                await SendEmailAsync(email, "🔔 Project Expired Notification", $"Your project with ID {projectId} has been deleted after feedback submission.");
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var smtp = _config.GetSection("SmtpSettings");
            var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]))
            {
                Credentials = new NetworkCredential(smtp["Username"], smtp["Password"]),
                EnableSsl = bool.Parse(smtp["EnableSsl"])
            };

            var message = new MailMessage
            {
                From = new MailAddress(smtp["Username"], "SonicPoints"),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(toEmail);

            await client.SendMailAsync(message);
        }
    }
}
