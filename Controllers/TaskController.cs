using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SonicPoints.Dto;
using SonicPoints.DTOs;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using System.Security.Claims;
using System.Text.Json;
using System.Text;
using System.Net.Http.Headers;

namespace SonicPoints.Controllers
{
    [Route("api/tasks")]
    [ApiController]
    [Authorize]
    public class TaskController : ControllerBase
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ILeaderboardRepository _leaderboardRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly IChatRepository _chatRepository;



        public TaskController(
            ITaskRepository taskRepository,
            ILeaderboardRepository leaderboardRepository,
            IProjectRepository projectRepository,
            IProjectAuthorizationService projectAuthorization,
            IHttpClientFactory httpClientFactory,
            IConfiguration config,
            IChatRepository chatRepository)
        {
            _taskRepository = taskRepository;
            _leaderboardRepository = leaderboardRepository;
            _projectRepository = projectRepository;
            _projectAuthorization = projectAuthorization;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _chatRepository = chatRepository;
        }

        private TaskDto MapToDto(TaskItem t) => new TaskDto
        {
            Id = t.Id,
            Title = t.Title,
            Description = t.Description,
            Status = t.Status.ToString(),
            Priority = t.Priority.ToString(),
            ProjectId = t.ProjectId,
            RewardPoints = t.RewardPoints,
            DueDate = t.DueDate,
            AssignedUserId = t.UserId,
            AssignedUserName = t.User?.UserName ?? "Unassigned"

        };

        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] CreateTaskDto createTaskDto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!await _projectAuthorization.HasProjectRoleAsync(userId, createTaskDto.ProjectId, "Admin", "Manager"))
                    return Forbid("🚫 You do not have permission to add tasks in this project.");

                var task = new TaskItem
                {
                    Title = createTaskDto.Title,
                    Description = createTaskDto.Description,
                    Status = ProjectTaskStatus.Backlog,
                    Priority = createTaskDto.Priority,
                    ProjectId = createTaskDto.ProjectId,
                    RewardPoints = createTaskDto.RewardPoints,
                    AssignedDate = DateTime.UtcNow,
                    DueDate = createTaskDto.DueDate,
                    UserId = userId
                };

                var createdTask = await _taskRepository.CreateTaskAsync(task, userId);
                return CreatedAtAction(nameof(GetTaskById), new { taskId = createdTask.Id }, MapToDto(createdTask));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("{taskId}/status")]
        public async Task<IActionResult> UpdateTaskStatus(int taskId, [FromBody] UpdateTaskDto updateTaskDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var task = await _taskRepository.GetTaskByIdAsync(taskId);

            if (task == null)
                return NotFound("❌ Task not found.");

            if (updateTaskDto.Status == (int)ProjectTaskStatus.Completed)
            {
                var allowed = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Admin", "Manager", "Checker");
                if (!allowed)
                    return Forbid("⛔ You do not have permission to complete this task.");
            }

            task.Status = (ProjectTaskStatus)updateTaskDto.Status;
            var updated = await _taskRepository.UpdateTaskStatusAsync(task);
            if (!updated)
                return BadRequest("❌ Failed to update task status.");

            return Ok(MapToDto(task));
        }

        [HttpPost("{taskId}/check")]
        public async Task<IActionResult> CheckTaskCompletion(int taskId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var task = await _taskRepository.GetTaskByIdAsync(taskId);

            if (task == null || task.Status != ProjectTaskStatus.Review)
                return BadRequest("⚠️ Task is either not found or not in 'Review' status.");

            var authorized = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Admin", "Manager", "Checker");
            if (!authorized)
                return Forbid("⛔ You do not have permission to complete this task.");

            task.Status = ProjectTaskStatus.Completed;

            var pointUserId = task.PointEligibleUserId ?? userId;

            var leaderboardEntry = await _leaderboardRepository.GetLeaderboardEntry(taskId, pointUserId);

            if (leaderboardEntry == null)
            {
                leaderboardEntry = new Leaderboard
                {
                    UserId = pointUserId,
                    TaskId = taskId,
                    PointsEarned = task.RewardPoints,
                    DateCompleted = DateTime.UtcNow
                };
                await _leaderboardRepository.AddLeaderboardEntry(leaderboardEntry);
            }
            else
            {
                leaderboardEntry.PointsEarned += task.RewardPoints;
                leaderboardEntry.DateCompleted = DateTime.UtcNow;
                await _leaderboardRepository.UpdateLeaderboardEntry(leaderboardEntry);
            }


            await _taskRepository.SaveAsync();
            return Ok("✅ Task completed and points awarded.");
        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> EditTask(int taskId, [FromBody] EditTaskDto dto)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var task = await _taskRepository.GetTaskByIdAsync(taskId);

                if (task == null)
                    return NotFound(" Task not found.");

                var allowed = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Admin", "Manager");
                if (!allowed)
                    return Forbid(" You do not have permission to edit tasks.");

                task.Title = dto.Title;
                task.Description = dto.Description;
                task.Priority = dto.Priority;
                task.DueDate = dto.DueDate;
                task.RewardPoints = dto.RewardPoints;

                var updated = await _taskRepository.UpdateTaskAsync(task);
                if (!updated)
                    return BadRequest(" Failed to update task.");

                return Ok(MapToDto(task));
            }
            catch (Exception ex)
            {
                Console.WriteLine("EditTask error: " + ex.Message);
                return StatusCode(500, " Server error: " + ex.Message);
            }
        }
        // ... [All existing usings and namespace remain unchanged]

        [HttpPost("reorder")]
        public async Task<IActionResult> ReorderTasks([FromBody] List<TaskOrderDto> list)
        {
            if (list == null || list.Count == 0)
                return BadRequest("⚠️ No tasks provided for reordering.");

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            

            var updatedTasks = new List<TaskItem>();

            foreach (var item in list)
            {
                if (item == null || item.TaskId <= 0)
                    continue;

                var task = await _taskRepository.GetTaskByIdAsync(item.TaskId);
                if (task == null)
                    continue;

                if (!Enum.IsDefined(typeof(ProjectTaskStatus), item.NewStatus))
                    return BadRequest($"⚠️ Invalid status value {item.NewStatus} for task {item.TaskId}.");

                var newStatus = (ProjectTaskStatus)item.NewStatus;
                var currentStatus = task.Status;

                bool isAdmin = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Admin");
                bool isManager = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Manager");
                bool isChecker = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Checker");
                bool isMember = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Member");

                bool canTransition = false;

                if (isAdmin || isManager)
                {
                    // Admin/Manager can do anything except marking completed from non-review
                    canTransition = currentStatus != ProjectTaskStatus.Completed || newStatus != ProjectTaskStatus.Backlog;
                    if (newStatus == ProjectTaskStatus.Completed && currentStatus != ProjectTaskStatus.Review)
                        return BadRequest("✅ Admin/Manager can only mark Completed from Review.");
                }
                else if (isChecker)
                {
                    if (newStatus == ProjectTaskStatus.Backlog)
                        canTransition = true;
                    else if (newStatus == ProjectTaskStatus.Completed && currentStatus == ProjectTaskStatus.Review)
                        canTransition = true;
                }
                else if (isMember)
                {
                    if (currentStatus == ProjectTaskStatus.Backlog && newStatus == ProjectTaskStatus.InProgress)
                        canTransition = true;
                    else if (currentStatus == ProjectTaskStatus.InProgress && newStatus == ProjectTaskStatus.Review)
                        canTransition = true;
                    else if (currentStatus == ProjectTaskStatus.InProgress && newStatus == ProjectTaskStatus.Backlog)
                        canTransition = true;
                }

                if (!canTransition)
                    return Forbid("❌ You are not authorized to make this transition.");

                task.Status = newStatus;

                // Assign or clear user depending on status
                if (newStatus == ProjectTaskStatus.InProgress)
                {
                    task.UserId = userId;
                    task.PointEligibleUserId = userId; // Track who will earn points
                }
                else if (newStatus == ProjectTaskStatus.Review || newStatus == ProjectTaskStatus.Completed)
                {
                    if (string.IsNullOrEmpty(task.UserId))
                        task.UserId = userId;
                }
               
                else if (newStatus == ProjectTaskStatus.Backlog)
                {
                    task.UserId = null;
                }

                await _taskRepository.UpdateTaskAsync(task);
                updatedTasks.Add(task);
            }

            await _taskRepository.SaveAsync();
            return Ok(updatedTasks.Select(MapToDto));
        }


        [HttpGet("project/{projectId}/progress-trend")]
        public async Task<IActionResult> GetProjectProgressTrend(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker", "Member"))
                return Forbid();

            var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId);

            var trend = tasks
                .Where(t => t.Status == ProjectTaskStatus.Completed)
                .GroupBy(t => t.AssignedDate.Date) 
                .OrderBy(g => g.Key)
                .Select(g => new {
                    date = g.Key.ToString("yyyy-MM-dd"), 
                    count = g.Count()
                });

            return Ok(trend);
        }



        [HttpGet("project/{projectId}/analytics")]
        public async Task<IActionResult> GetTaskAnalytics(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager"))
                return Forbid("⛔ You do not have permission to view analytics.");

            var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId);
            var grouped = tasks
                .GroupBy(t => t.User?.Email ?? "Unassigned")
                .Select(g => new {
                    User = g.Key,
                    TaskCount = g.Count(),
                    Completed = g.Count(t => t.Status == ProjectTaskStatus.Completed)
                });

            return Ok(grouped);
        }

        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetTasksByProject(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker", "Member"))
                return Forbid("⛔ You do not have permission to view tasks in this project.");

            var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId, userId);
            return Ok(tasks.Select(MapToDto));
        }




        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTaskById(int taskId)
        {
            var task = await _taskRepository.GetTaskByIdAsync(taskId);
            if (task == null)
                return NotFound("❌ Task not found.");

            return Ok(MapToDto(task));
        }

        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var task = await _taskRepository.GetTaskByIdAsync(taskId);

            if (task == null)
                return NotFound("❌ Task not found.");

            var allowed = await _projectAuthorization.HasProjectRoleAsync(userId, task.ProjectId, "Admin", "Manager");
            if (!allowed)
                return Forbid("⛔ You do not have permission to delete tasks.");

            var deleted = await _taskRepository.DeleteTaskAsync(taskId, userId);
            if (!deleted)
                return BadRequest("❌ Failed to delete task.");

            return NoContent();
        }

        [HttpGet("project/{projectId}/status-counts")]
        public async Task<IActionResult> GetTaskStatusCounts(int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!await _projectAuthorization.HasProjectRoleAsync(userId, projectId, "Admin", "Manager", "Checker", "Member"))
                return Forbid("⛔ You do not have access to this project.");

            var tasks = await _taskRepository.GetTasksByProjectIdAsync(projectId);

            var result = new Dictionary<string, int>
            {
                ["Backlog"] = 0,
                ["InProgress"] = 0,
                ["Review"] = 0,
                ["Completed"] = 0
            };

            foreach (var task in tasks)
            {
                var statusName = task.Status.ToString();
                if (result.ContainsKey(statusName))
                    result[statusName]++;
            }

            return Ok(result);
        }


        [HttpPost("assistant")]
        public async Task<IActionResult> TaskAssistant([FromBody] ChatRequestDto request, int projectId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Retrieve previous chat history from database
            var previousMessages = await _chatRepository.GetChatHistoryAsync(projectId, userId);

            var messages = previousMessages.Select(m => new { role = m.Role, content = m.Content }).ToList();

            messages.Add(new { role = "user", content = request.Message });

            var payload = new
            {
                model = "gpt-3.5-turbo",
                messages = messages
            };

            // Send request to OpenAI API
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _config["OpenAI:ApiKey"]);

            var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var reply = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

            // Save user message and assistant response to database
            await _chatRepository.SaveChatMessageAsync(new ChatMessage
            {
                UserId = userId,
                ProjectId = projectId,
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow
            });

            await _chatRepository.SaveChatMessageAsync(new ChatMessage
            {
                UserId = userId,
                ProjectId = projectId,
                Role = "assistant",
                Content = reply,
                Timestamp = DateTime.UtcNow
            });

            return Ok(new { reply });
        }


    }
}
