using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SonicPoints.Data;
using SonicPoints.Dto;
using SonicPoints.DTOs;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SonicPoints.Controllers
{
    [Route("api/projects")]
    [ApiController]
    [Authorize]
    public class ProjectController : ControllerBase
    {
        private readonly IProjectRepository _projectRepository;
        private readonly UserManager<User> _userManager;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;



        public ProjectController(
            IProjectRepository projectRepository,
            UserManager<User> userManager,
            IProjectAuthorizationService projectAuthorization,
            IConfiguration configuration,

            AppDbContext context)
        {
            _projectRepository = projectRepository;
            _userManager = userManager;
            _projectAuthorization = projectAuthorization;
            _configuration = configuration;
            _context = context;
        }
        [HttpGet("{projectId}/my-role")]
        public async Task<IActionResult> GetMyRole(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var projectUser = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.ProjectId == projectId && pu.UserId == userId);

            if (projectUser == null)
                return NotFound("You are not part of this project.");

            return Ok(new { role = projectUser.Role });
        }


        [HttpGet]
        public async Task<IActionResult> GetUserProjects()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("❌ Invalid token: No user ID found in claims.");

            var projects = await _projectRepository.GetUserProjectsAsync(userId);
            var projectDtos = projects.Select(p => new ProjectDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                DueDate = p.DueDate,
                ProjectStatus = p.ComputedProjectStatus,
                Progress = p.Progress
            });

            return Ok(projectDtos);
        }
        [HttpGet("{projectId}/users")]
        public async Task<IActionResult> GetProjectUsers(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var hasAccess = await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker");
            if (!hasAccess)
                return Forbid("Access denied.");

            var users = await _context.ProjectUsers
                .Where(pu => pu.ProjectId == projectId)
                .Include(pu => pu.User)
                .Select(pu => new
                {
                    Id = pu.UserId,
                    Name = pu.User != null ? pu.User.UserName : "Unknown",
                    Email = pu.User != null ? pu.User.Email : "Unknown",
                    KpiPoints = pu.RewardPoints,
                    Role = pu.Role
                })
                .ToListAsync();

            return Ok(users);
        }



        [HttpGet("{id}")]
        public async Task<IActionResult> GetProject(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var project = await _projectRepository.GetProjectByIdAsync(id, userId);

            if (project == null)
                return NotFound("Project not found or you don't have access.");

            var projectDto = new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                DueDate = project.DueDate,
                ProjectStatus = project.ComputedProjectStatus,
                Progress = project.Progress
            };

            return Ok(projectDto);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProject([FromBody] CreateProjectDto createProjectDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            var project = new Project
            {
                Name = createProjectDto.Name,
                Description = createProjectDto.Description,
                DueDate = createProjectDto.DueDate,
                AdminId = userId,
                ProjectStatus = "Not Started"
            };

            var createdProject = await _projectRepository.CreateProjectAsync(project, userId);

            var projectDto = new ProjectDto
            {
                Id = createdProject.Id,
                Name = createdProject.Name,
                Description = createdProject.Description,
                DueDate = createdProject.DueDate,
                ProjectStatus = createdProject.ProjectStatus,
                Progress = createdProject.Progress
            };

            // Send email
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var smtpHost = smtpSettings["Host"];
            var smtpPort = int.Parse(smtpSettings["Port"]);
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];
            var smtpEnableSsl = bool.Parse(smtpSettings["EnableSsl"]);

            var subject = "✅ New Project Created Successfully";
            var body = $@"
        <h2>Project Created</h2>
        <p>Dear User,</p>
        <p>Your project has been successfully created with the following details:</p>
        <ul>
            <li><strong>Name:</strong> {projectDto.Name}</li>
            <li><strong>Description:</strong> {projectDto.Description}</li>
            <li><strong>Due Date:</strong> {projectDto.DueDate:yyyy-MM-dd}</li>
            <li><strong>Status:</strong> {projectDto.ProjectStatus}</li>
            <li><strong>Progress:</strong> {projectDto.Progress}%</li>
        </ul>
        <p>Please manage your project accordingly using the system dashboard.</p>
        <br/>
        <p>Best regards,<br/>SonicPoints Team</p>
    ";

            using var smtpClient = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                EnableSsl = smtpEnableSsl
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUsername, "SonicPoints"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(userEmail);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log or handle the error silently; do not block project creation
                Console.WriteLine("Email sending failed: " + ex.Message);
            }

            return CreatedAtAction(nameof(GetProject), new { id = projectDto.Id }, projectDto);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProject(int id, [FromBody] UpdateProjectDto updateProjectDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, id, "Admin", "Manager"))
                return Forbid("You are not authorized to update this project.");

            var project = await _projectRepository.UpdateProjectAsync(id, userId, updateProjectDto);

            if (project == null)
                return NotFound("Project not found or you don't have permission to update.");

            return Ok(new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                DueDate = project.DueDate,
                ProjectStatus = project.ProjectStatus,
                Progress = project.Progress
            });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProject(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, id, "Admin"))
                return Forbid("Only project Admins can delete this project.");

            var success = await _projectRepository.DeleteProjectAsync(id, userId);

            if (!success)
                return NotFound("Project not found or you don't have permission to delete.");

            return NoContent();
        }

        [HttpPost("{id}/add-user")]
        public async Task<IActionResult> AddUserToProject(int id, [FromBody] string userEmail)
        {
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(adminId, id, "Admin"))
                return Forbid("Only project Admins can add users.");

            var success = await _projectRepository.AddUserToProjectAsync(id, adminId, userEmail);

            if (!success)
                return BadRequest("Failed to add user to project. Check if the email is valid and you're an admin.");

            return Ok("User added successfully.");
        }

        [HttpPut("{projectId}/change-role")]
        public async Task<IActionResult> ChangeUserRole(int projectId, [FromBody] ChangeRoleDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin"))
                return Forbid("Only project Admins can change roles.");

            var user = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.UserId == dto.TargetUserId && pu.ProjectId == projectId);

            if (user == null)
                return NotFound("User not found in project.");

            user.Role = dto.NewRole;
            await _context.SaveChangesAsync();

            return Ok("Role updated.");
        }
        [HttpGet("points/{projectId}")]
        public async Task<IActionResult> GetUserPoints(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var points = await _context.ProjectUserPoints
                .Where(p => p.ProjectId == projectId && p.UserId == userId)
                .Select(p => p.TotalPoints)
                .FirstOrDefaultAsync();

            return Ok(new { points });
        }


        [HttpDelete("{projectId}/remove-user/{targetUserId}")]
        public async Task<IActionResult> RemoveUserFromProject(int projectId, string targetUserId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin"))
                return Forbid("Only project Admins can remove users.");

            var user = await _context.ProjectUsers
                .FirstOrDefaultAsync(pu => pu.UserId == targetUserId && pu.ProjectId == projectId);

            if (user == null)
                return NotFound("User not found in project.");

            _context.ProjectUsers.Remove(user);
            await _context.SaveChangesAsync();

            return Ok("User removed.");
        }
    }
}