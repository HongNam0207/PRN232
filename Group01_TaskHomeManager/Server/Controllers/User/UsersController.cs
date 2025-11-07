using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.DTOs;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public UsersController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                Role = _context.Roles.FirstOrDefault(r => r.RoleId == user.RoleId)?.RoleName ?? "Member"
            });
        }

        [HttpPut("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDTO updated)
        {
            if (updated == null)
                return BadRequest(new { message = "Dữ liệu không hợp lệ." });

            var userId = int.Parse(User.FindFirst("UserId")!.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound();

            user.FullName = updated.FullName ?? user.FullName;
            user.PhoneNumber = updated.PhoneNumber ?? user.PhoneNumber;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Cập nhật thông tin thành công!" });
        }
    }
}
