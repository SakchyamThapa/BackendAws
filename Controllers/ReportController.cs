using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.Dto;
using SonicPoints.Models;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Text;

namespace SonicPoints.Controllers
{
    [Route("api/reports")]
    [ApiController]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportController(AppDbContext context)
        {
            _context = context;
        }

        // Generate personal user report
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateReport()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var tasksCompleted = await _context.Tasks
                .CountAsync(t => t.UserId == userId && t.Status == ProjectTaskStatus.Completed);

            var totalPoints = await _context.Leaderboards
                .Where(l => l.UserId == userId)
                .SumAsync(l => (int?)l.PointsEarned) ?? 0;

            var rewards = await _context.RedeemHistory
                .CountAsync(r => r.UserId == userId);

            var username = await _context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync() ?? "Unknown";

            var content = $"User Report for {username}\n" +
                          $"- Tasks Completed: {tasksCompleted}\n" +
                          $"- Total Points Earned: {totalPoints}\n" +
                          $"- Rewards Redeemed: {rewards}\n" +
                          $"- Report Generated At: {DateTime.UtcNow}";

            var report = new Report
            {
                UserId = userId,
                ReportContent = content,
                GeneratedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new ReportDto
            {
                ReportId = report.ReportId,
                UserId = int.TryParse(userId, out var uid) ? uid : 0,
                ReportContent = report.ReportContent,
                GeneratedAt = report.GeneratedAt,
                User = username
            });
        }


        [Authorize(Roles = "Admin,Manager,SuperAdmin")]
        [HttpGet("project/{projectId}/download-all-reports")]
        public async Task<IActionResult> DownloadAllReportsAsPdf(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound("Project not found.");

            var users = await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId)
                .Include(pu => pu.User)
                .ToListAsync();

            if (!users.Any())
                return NotFound("No users in this project.");

            var builder = new StringBuilder();

            builder.AppendLine($"Project Report: {project.Name}");
            builder.AppendLine($"Generated At: {DateTime.UtcNow}");
            builder.AppendLine(new string('-', 80));

            foreach (var pu in users)
            {
                var uid = pu.UserId;
                var username = pu.User?.UserName ?? "Unknown";

                var tasksCompleted = await _context.Tasks
                    .CountAsync(t => t.ProjectId == projectId && t.UserId == uid && t.Status == ProjectTaskStatus.Completed);

                var points = await _context.Leaderboards
                    .Where(l => l.Task.ProjectId == projectId && l.UserId == uid)
                    .SumAsync(l => (int?)l.PointsEarned) ?? 0;

                var rewards = await _context.RedeemHistory
                    .CountAsync(r => r.ProjectId == projectId && r.UserId == uid);

                builder.AppendLine($"👤 {username} (UserID: {uid})");
                builder.AppendLine($"- Tasks Completed: {tasksCompleted}");
                builder.AppendLine($"- Points Earned: {points}");
                builder.AppendLine($"- Rewards Redeemed: {rewards}");
                builder.AppendLine(new string('-', 50));
            }

            var pdfBytes = GeneratePdfBytes(builder.ToString());
            var fileName = $"project_{projectId}_report.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // 👇 Place this method inside the same controller class
        private byte[] GeneratePdfBytes(string content)
        {
            var document = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(50);
                    page.Header().Text("SonicPoints Project Report").SemiBold().FontSize(20);
                    page.Content().Text(content).FontSize(12);
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by SonicPoints • Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            });

            return document.GeneratePdf();
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAllReports()
        {
            var reports = await _context.Reports
                .OrderByDescending(r => r.GeneratedAt)
                .ToListAsync();
            return Ok(reports);
        }


        //  Generate detailed report for a specific project
        [HttpPost("project/{projectId}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GenerateProjectReport(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                return NotFound("Project not found.");

            var users = await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId)
                .Include(pu => pu.User)
                .ToListAsync();

            if (!users.Any())
                return NotFound("No users found in this project.");

            var reportLines = new List<string>
            {
                $"Project Report for '{project.Name}'",
                $" Generated At: {DateTime.UtcNow}",
                $" Total Users: {users.Count}",
                $"-------------------------------------------------------------"
            };

            foreach (var pu in users)
            {
                var uid = pu.UserId;
                var userName = pu.User?.UserName ?? "Unknown";

                var tasks = await _context.Tasks
                    .Where(t => t.ProjectId == projectId && t.UserId == uid)
                    .ToListAsync();

                var countBacklog = tasks.Count(t => t.Status == ProjectTaskStatus.Backlog);
                var countInProgress = tasks.Count(t => t.Status == ProjectTaskStatus.InProgress);
                var countReview = tasks.Count(t => t.Status == ProjectTaskStatus.Review);
                var countCompleted = tasks.Count(t => t.Status == ProjectTaskStatus.Completed);
                var totalTasks = tasks.Count;

                var rewardCount = await _context.RedeemHistory
                    .CountAsync(r => r.ProjectId == projectId && r.UserId == uid);

                reportLines.Add(
                    $" {userName} (ID: {uid})\n" +
                    $"- Tasks: Backlog={countBacklog}, InProgress={countInProgress}, Review={countReview}, Completed={countCompleted}\n" +
                    $"- Total Tasks: {totalTasks}\n" +
                    $"- Rewards Redeemed: {rewardCount}\n"
                );
            }

            var reportText = string.Join("\n", reportLines);

            var report = new Report
            {
                UserId = userId,
                ReportContent = reportText,
                GeneratedAt = DateTime.UtcNow
            };

            _context.Reports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                projectId,
                projectName = project.Name,
                generatedBy = userId,
                reportId = report.ReportId,
                report = reportText
            });
        }
    }
}
