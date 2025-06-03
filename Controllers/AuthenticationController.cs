using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using SonicPoints.DTOs;
using SonicPoints.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using System.Text;

namespace SonicPoints.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;

        public AuthenticationController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { success = false, message = "Email already in use" });

            var user = new User
            {
                UserName = model.Username,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { success = false, errors = result.Errors });

            await _userManager.AddToRoleAsync(user, "Member");

            // Send registration email with login link
            var loginUrl = "https://localhost:7150/index.html"; // Adjust if needed
            var emailBody = $"Hello {user.UserName},<br><br>Your account has been successfully registered! You can log in using the following link:<br><a href='{loginUrl}'>Login Here</a>";

            // SMTP settings from appsettings.json
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var smtpHost = smtpSettings["Host"];
            var smtpPort = int.Parse(smtpSettings["Port"]);
            var smtpUsername = smtpSettings["Username"];
            var smtpPassword = smtpSettings["Password"];
            var smtpEnableSsl = bool.Parse(smtpSettings["EnableSsl"]);

            using (var smtpClient = new SmtpClient(smtpHost, smtpPort))
            {
                smtpClient.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                smtpClient.EnableSsl = smtpEnableSsl;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(smtpUsername),
                    Subject = "Account Registered and Ready to Login",
                    Body = emailBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(user.Email);

                try
                {
                    await smtpClient.SendMailAsync(mailMessage);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { success = false, message = "Error sending email: " + ex.Message });
                }
            }

            return Ok(new { success = true, message = "Registration successful", role = "Member" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { success = false, message = "Invalid email or password" });

            var token = await GenerateJwtToken(user);

            return Ok(new
            {
                success = true,
                message = "Login successful",
                token,
                userId = user.Id,
                username = user.UserName,
                email = user.Email
            });
        }

        [Authorize]
        [HttpGet("protected")]
        public IActionResult ProtectedRoute()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            return Ok(new
            {
                message = "✅ You are authorized!",
                userId,
                username,
                roles
            });
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = jwtSettings.GetValue<string>("Key") ?? throw new InvalidOperationException("JWT key is missing");
            var issuer = jwtSettings.GetValue<string>("Issuer") ?? throw new InvalidOperationException("JWT issuer is missing");
            var audience = jwtSettings.GetValue<string>("Audience") ?? throw new InvalidOperationException("JWT audience is missing");
            var expiryMinutes = jwtSettings.GetValue<int?>("ExpiryInMinutes") ?? 60;

            var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpGet("analyze-token")]
        public IActionResult AnalyzeToken()
        {
            if (!Request.Headers.TryGetValue("Authorization", out var bearerToken))
                return Unauthorized(new { success = false, message = "No Authorization header found" });

            var tokenStr = bearerToken.ToString().Replace("Bearer ", "").Trim();
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = jwtSettings.GetValue<string>("Key") ?? "";

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(tokenStr))
                    return BadRequest(new { tokenValid = false, message = "Invalid token format" });

                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.GetValue<string>("Issuer"),
                    ValidAudience = jwtSettings.GetValue<string>("Audience"),
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                ClaimsPrincipal principal = handler.ValidateToken(tokenStr, validationParams, out var validatedToken);

                return Ok(new
                {
                    tokenValid = true,
                    issuer = ((JwtSecurityToken)validatedToken).Issuer,
                    audience = ((JwtSecurityToken)validatedToken).Audiences.FirstOrDefault(),
                    expiration = validatedToken.ValidTo,
                    claims = principal.Claims.Select(c => new { c.Type, c.Value })
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { tokenValid = false, error = ex.Message });
            }
        }

        [HttpGet("debug")]
        public IActionResult Debug()
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            return Ok(new
            {
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                KeyLength = jwtSettings["Key"]?.Length ?? 0,
                ExpiryInMinutes = jwtSettings.GetValue<int?>("ExpiryInMinutes") ?? 60
            });
        }
    }
}
