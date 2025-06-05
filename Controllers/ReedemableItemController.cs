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
using System.Text.Json;
using System.Text.Json.Serialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;


namespace SonicPoints.Controllers
{
    [Route("api/redeemableitems")]
    [ApiController]
    [Authorize]
    public class RedeemableItemController : ControllerBase
    {
        private readonly IRedeemableItemRepository _redeemableItemRepository;
        private readonly IProjectAuthorizationService _projectAuthorization;
        private const int MaxImageWidth = 1024;
        private readonly AppDbContext _context;
        private readonly ILogger<RedeemableItemController> _logger;

        public RedeemableItemController(
            IRedeemableItemRepository redeemableItemRepository,
            IProjectAuthorizationService projectAuthorization,
            AppDbContext context,
            ILogger<RedeemableItemController> logger)
        {
            _redeemableItemRepository = redeemableItemRepository;
            _projectAuthorization = projectAuthorization;
            _context = context;
            _logger = logger;
        }

        public class ImgBBResponse
        {
            public ImgBBData Data { get; set; }
            public bool Success { get; set; }
            public int Status { get; set; }
        }

        public class ImgBBData
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string UrlViewer { get; set; }
            [JsonPropertyName("url")]
            public string Url { get; set; }
            public string DisplayUrl { get; set; }
            public string Width { get; set; }
            public string Height { get; set; }
            public string Size { get; set; }
            public string Time { get; set; }
            public string Expiration { get; set; }
            public ImgBBImage Image { get; set; }
            public ImgBBImage Thumb { get; set; }
            public ImgBBImage Medium { get; set; }
            public string DeleteUrl { get; set; }
        }

        public class ImgBBImage
        {
            public string Filename { get; set; }
            public string Name { get; set; }
            public string Mime { get; set; }
            public string Extension { get; set; }
            public string Url { get; set; }
        }

        private async Task<byte[]> ResizeImage(IFormFile image, int maxWidth = MaxImageWidth)
        {
            using var imageStream = image.OpenReadStream();
            using var imageToResize = SixLabors.ImageSharp.Image.Load(imageStream);

            // Resize the image to the max width while maintaining the aspect ratio
            if (imageToResize.Width > maxWidth)
            {
                imageToResize.Mutate(x => x.Resize(maxWidth, 0)); // Resize keeping aspect ratio
            }

            using var outputStream = new MemoryStream();
            imageToResize.Save(outputStream, new JpegEncoder()); // Save as JPEG
            return outputStream.ToArray();
        }

        // Upload image to ImgBB and get URL
        private async Task<string> UploadToImgBB(IFormFile file)
        {
            string apiKey = "9fad27fa86d50d998945b30537ec274d";  // Replace with your actual API key
            string url = $"https://api.imgbb.com/1/upload?key={apiKey}";

            using var client = new HttpClient();
            using var content = new MultipartFormDataContent();

            // Create a StreamContent for the file
            var imageContent = new StreamContent(file.OpenReadStream());
            content.Add(imageContent, "image", file.FileName);

            // Post the request to ImgBB API
            var response = await client.PostAsync(url, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"ImgBB upload failed: {await response.Content.ReadAsStringAsync()}");
                return null;
            }

            // Deserialize the response body
            var responseBody = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("ImgBB Response: {ResponseBody}", responseBody);

            try
            {
                // Manually parse the response body using JsonDocument
                var jsonResponse = JsonDocument.Parse(responseBody);

                // Extract the URL from the response
                var imgUrl = jsonResponse.RootElement
                    .GetProperty("data")
                    .GetProperty("url")
                    .GetString();

                // Check if the URL exists and return it
                if (!string.IsNullOrEmpty(imgUrl))
                {
                    _logger.LogInformation("ImgBB Image URL: {ImgUrl}", imgUrl);
                    return imgUrl;
                }
                else
                {
                    _logger.LogError("ImgBB response does not contain a valid URL.");
                    return null;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError($"Failed to parse ImgBB response: {ex.Message}");
                return null;
            }
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

                var uploadedUrl = await UploadToImgBB(dto.ImageUrl);
                if (string.IsNullOrEmpty(uploadedUrl))
                    return StatusCode(500, "Image upload failed.");

                var redeemableItem = new RedeemableItem
                {
                    Name = dto.Name,
                    Cost = dto.Cost,
                    ProjectId = dto.ProjectId,
                    ImageUrl = uploadedUrl,
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
                    var uploadedUrl = await UploadToImgBB(dto.ImageUrl);
                    if (string.IsNullOrEmpty(uploadedUrl))
                        return StatusCode(500, "Image upload failed.");

                    redeemableItem.ImageUrl = uploadedUrl;
                }

                var updatedItem = await _redeemableItemRepository.UpdateRedeemableItemAsync(redeemableItem);
                return Ok(updatedItem);
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

                return Ok(items.Select(item => new
                {
                    item.Id,
                    item.Name,
                    item.Cost,
                    item.ProjectId,
                    imageUrl = item.ImageUrl
                }));
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
                    imageUrl = item.ImageUrl
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
