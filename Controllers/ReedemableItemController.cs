using Azure.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SonicPoints.Data;
using SonicPoints.Dto.SonicPoints.Dto;
using SonicPoints.Models;
using SonicPoints.Repositories;
using SonicPoints.Services;
using System.Globalization;
using System.Security.Claims;

namespace SonicPoints.Controllers
{
    [Route("api/redeemableitems")]
    [ApiController]
    [Authorize]
    public class RedeemableItemController : ControllerBase
    {
        private readonly IRedeemableItemRepository _redeemableItemRepository;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private readonly AppDbContext _context;

        public RedeemableItemController(
            IRedeemableItemRepository redeemableItemRepository,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context)
        {
            _redeemableItemRepository = redeemableItemRepository;
            _projectAuthorization = projectAuthorization;
            _context = context;
        }




        [HttpPost("CreateRedeemableItem")]
        [RequestSizeLimit(30 * 1024 * 1024)]

        public async Task<IActionResult> CreateRedeemableItem([FromForm] RedeemableItemDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(dto.Name) || dto.Cost <= 0 || dto.ProjectId <= 0 || dto.ImageUrl == null)
                    return BadRequest("All fields and the image file are required.");

                var authorized = await _projectAuthorization.HasProjectRoleAsync(userId!, dto.ProjectId, "Admin", "Manager");
                if (!authorized)
                    return Forbid("You are not authorized to create redeemable items for this project.");

                var projectRoot = Directory.GetCurrentDirectory();
                var uploadFolder = Path.Combine(projectRoot, "Redeemable Items");
                Directory.CreateDirectory(uploadFolder);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                var originalFileName = Path.GetFileName(dto.ImageUrl.FileName);
                var fileName = $"{timestamp}_{originalFileName}";
                var filePath = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await dto.ImageUrl.CopyToAsync(stream);
                }

                var storedPath = Path.Combine("Redeemable Items", fileName);

                var redeemableItem = new RedeemableItem
                {
                    Name = dto.Name,
                    Cost = dto.Cost,
                    ProjectId = dto.ProjectId,
                    ImageUrl = storedPath,
                    Quantity = dto.Quantity
                };

                var createdItem = await _redeemableItemRepository.AddRedeemableItemAsync(redeemableItem);
                if (createdItem == null)
                    return StatusCode(500, "Failed to create the redeemable item.");

                return CreatedAtAction(nameof(GetRedeemableItemById), new { id = createdItem.Id }, createdItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
        }


        [HttpGet("project/{projectId}")]
        public async Task<IActionResult> GetRedeemableItemsByProjectId(int projectId)
        {
            try
            {
                var items = await _redeemableItemRepository.GetRedeemableItemsByProjectId(projectId);

                if (items == null || !items.Any())
                    return Ok(items.Select(item => new
                    {
                        item.Id,
                        item.Name,
                        item.Cost,
                        item.ProjectId,
                        imageUrl = $"https://localhost:7150/{item.ImageUrl.Replace("\\", "/")}"
                    }));



                return Ok(items.Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Cost,
                    item.ProjectId,
                    imageUrl = $"https://localhost:7150/{item.ImageUrl.Replace("\\", "/")}"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
        }



        [HttpPut("{id}")]
        [RequestSizeLimit(30 * 1024 * 1024)]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateRedeemableItem(int id, [FromForm] RedeemableItemDto dto)
        {
            try
            {
                var redeemableItem = await _redeemableItemRepository.GetRedeemableItemByIdAsync(id);
                if (redeemableItem == null)
                    return NotFound("Redeemable item not found.");

                redeemableItem.Name = dto.Name;
                redeemableItem.Cost = dto.Cost;

                if (dto.ImageUrl != null && dto.ImageUrl.Length > 0)
                {
                    var projectRoot = Directory.GetCurrentDirectory();
                    var uploadFolder = Path.Combine(projectRoot, "Redeemable Items");
                    Directory.CreateDirectory(uploadFolder);

                    var oldImagePath = Path.Combine(projectRoot, redeemableItem.ImageUrl ?? "");
                    if (System.IO.File.Exists(oldImagePath))
                        System.IO.File.Delete(oldImagePath);

                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
                    var newFileName = $"{timestamp}_{Path.GetFileName(dto.ImageUrl.FileName)}";
                    var newFilePath = Path.Combine(uploadFolder, newFileName);

                    using (var stream = new FileStream(newFilePath, FileMode.Create))
                    {
                        await dto.ImageUrl.CopyToAsync(stream);
                    }

                    redeemableItem.ImageUrl = Path.Combine("Redeemable Items", newFileName);
                }

                var updatedItem = await _redeemableItemRepository.UpdateRedeemableItemAsync(redeemableItem);
                return Ok(updatedItem);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetRedeemableItemById(int id)
        {
            try
            {
                var item = await _redeemableItemRepository.GetRedeemableItemByIdAsync(id);
                if (item == null)
                    return NotFound("Item not found.");

                return Ok(new
                {
                    item.Id,
                    item.Name,
                    item.Cost,
                    item.ProjectId,
                    imageUrl = $"https://localhost:7150/{item.ImageUrl.Replace("\\", "/")}"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
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


        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRedeemableItem(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var redeemableItem = await _redeemableItemRepository.GetRedeemableItemByIdAsync(id);
                if (redeemableItem == null)
                    return NotFound("Redeemable item not found.");

                var authorized = await _projectAuthorization.HasProjectRoleAsync(userId!, redeemableItem.ProjectId, "Admin", "Manager");
                if (!authorized)
                    return Forbid("You are not authorized to delete items in this project.");

                var fullImagePath = Path.Combine(Directory.GetCurrentDirectory(), redeemableItem.ImageUrl ?? "");
                if (System.IO.File.Exists(fullImagePath))
                    System.IO.File.Delete(fullImagePath);

                await _redeemableItemRepository.DeleteRedeemableItemAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Unexpected error occurred: {ex.Message}");
            }
        }
    }
}