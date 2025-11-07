using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.DTOs;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Member")]
    public class FamiliesController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public FamiliesController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/Families
        //    => Lấy danh sách tất cả các gia đình (có hỗ trợ filter/sort)
        // ============================================================
        [HttpGet]
        [EnableQuery]
        public IQueryable<FamilyReadDTO> GetFamilies()
        {
            return _context.Families
                .Include(f => f.CreatedByNavigation)
                .Select(f => new FamilyReadDTO
                {
                    FamilyId = f.FamilyId,
                    FamilyName = f.FamilyName,
                    Address = f.Address,
                    CreatedAt = f.CreatedAt ?? DateTime.MinValue,
                    CreatedByName = f.CreatedByNavigation != null
                        ? f.CreatedByNavigation.FullName
                        : "Unknown"
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. GET: api/Families/myfamilies
        //    => Lấy danh sách gia đình mà user hiện tại đang tham gia
        // ============================================================
        [HttpGet("myfamilies")]
        [EnableQuery]
        public IQueryable<FamilyReadDTO> GetMyFamilies()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value; // ✅ đổi đúng claim
            if (userIdClaim == null)
                return Enumerable.Empty<FamilyReadDTO>().AsQueryable();

            int userId = int.Parse(userIdClaim);

            return _context.FamilyMembers
                .Include(fm => fm.Family)
                .Include(fm => fm.User)
                .Where(fm => fm.UserId == userId)
                .Select(fm => new FamilyReadDTO
                {
                    FamilyId = fm.Family.FamilyId,
                    FamilyName = fm.Family.FamilyName,
                    Address = fm.Family.Address,
                    CreatedAt = fm.Family.CreatedAt ?? DateTime.Now,
                    Relationship = fm.Relationship
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 3. POST: api/Families
        //    => Tạo mới gia đình
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateFamily([FromBody] FamilyCreateDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value; // ✅ sửa đúng claim
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            if (string.IsNullOrWhiteSpace(dto.FamilyName))
                return BadRequest(new { message = "Tên gia đình không được để trống." });

            // 🔹 Tạo mới gia đình
            var family = new Family
            {
                FamilyName = dto.FamilyName,
                Address = dto.Address,
                CreatedBy = userId,
                CreatedAt = DateTime.Now
            };

            _context.Families.Add(family);
            await _context.SaveChangesAsync();

            // 🔹 Người tạo tự động trở thành thành viên
            var member = new FamilyMember
            {
                FamilyId = family.FamilyId,
                UserId = userId,
                Relationship = "Chủ gia đình",
                JoinDate = DateTime.Now
            };

            _context.FamilyMembers.Add(member);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tạo gia đình thành công.",
                data = new FamilyReadDTO
                {
                    FamilyId = family.FamilyId,
                    FamilyName = family.FamilyName,
                    Address = family.Address,
                    CreatedAt = family.CreatedAt ?? DateTime.Now,
                    CreatedByName = "Bạn",
                    Relationship = "Chủ gia đình"
                }
            });
        }

        // ============================================================
        // 🔹 4. PUT: api/Families/{id}
        //    => Cập nhật thông tin gia đình (chỉ người tạo được sửa)
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamily(int id, [FromBody] FamilyUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value; // ✅ sửa đúng claim
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var family = await _context.Families.FindAsync(id);
            if (family == null) return NotFound(new { message = "Không tìm thấy gia đình." });

            if (family.CreatedBy != userId)
                return Forbid();

            if (!string.IsNullOrEmpty(dto.FamilyName))
                family.FamilyName = dto.FamilyName;

            if (!string.IsNullOrEmpty(dto.Address))
                family.Address = dto.Address;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật gia đình thành công." });
        }

        // ============================================================
        // 🔹 5. DELETE: api/Families/{id}
        //    => Xóa gia đình (chỉ người tạo được xóa)
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamily(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value; // ✅ sửa đúng claim
            if (userIdClaim == null) return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var family = await _context.Families.FindAsync(id);
            if (family == null) return NotFound(new { message = "Không tìm thấy gia đình." });

            if (family.CreatedBy != userId)
                return Forbid();

            _context.Families.Remove(family);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xóa gia đình thành công." });
        }
    }
}
