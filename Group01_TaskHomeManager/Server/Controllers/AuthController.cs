using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.DTOs;
using Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

// ⚙️ Tạo alias để tránh xung đột với namespace System.Security.Principal.User
using UserEntity = Server.Models.User;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;
        private readonly IConfiguration _config;

        public AuthController(HomeTaskManagementDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // =====================================================
        // 🔹 REGISTER - Đăng ký người dùng (Không mã hóa password)
        // =====================================================
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email và mật khẩu không được để trống!" });

            if (await _context.Users.AnyAsync(u => u.Email == req.Email))
                return BadRequest(new { message = "Email đã được sử dụng!" });

            // 🔹 Lấy RoleId cho Member từ bảng Roles (đảm bảo tồn tại)
            var memberRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Member");
            if (memberRole == null)
                return StatusCode(500, new { message = "Không tìm thấy role 'Member' trong hệ thống!" });

            // ✅ Tạo người dùng mới với Role mặc định là Member
            var user = new UserEntity
            {
                FullName = req.FullName ?? "",
                Email = req.Email,
                PasswordHash = req.Password, // ❗ Không mã hóa
                RoleId = memberRole.RoleId,  // 🔸 gán động theo DB
                PhoneNumber = req.PhoneNumber,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đăng ký thành công! (Role mặc định: Member)" });
        }


        // =====================================================
        // 🔹 LOGIN - Đăng nhập, sinh JWT token
        // =====================================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Email và mật khẩu không được để trống!" });

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == req.Email && u.IsActive == true);

            if (user == null)
                return Unauthorized(new { message = "Email không tồn tại hoặc tài khoản bị khóa!" });

            if (user.PasswordHash != req.Password)
                return Unauthorized(new { message = "Sai mật khẩu!" });

            // 🔸 Tạo JWT token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("UserId", user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.FullName ?? ""),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(ClaimTypes.Role, user.Role?.RoleName ?? "Member")
                }),
                Expires = DateTime.UtcNow.AddHours(3),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var jwt = tokenHandler.WriteToken(token);

            return Ok(new
            {
                message = "Đăng nhập thành công!",
                token = jwt,
                user = new
                {
                    user.UserId,
                    user.FullName,
                    user.Email,
                    Role = user.Role?.RoleName
                }
            });
        }

        // =====================================================
        // 🔐 DEMO: API yêu cầu xác thực bằng JWT
        // =====================================================
        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Lấy UserId từ JWT
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            if (userId == null) return Unauthorized();

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId.ToString() == userId);

            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                Role = user.Role?.RoleName
            });
        }
    }
}
