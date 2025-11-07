using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Server.Models;
using Server.DTOs;
using System.Security.Claims;
using System.Linq;

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
        //    => Lấy danh sách tất cả các gia đình (admin hoặc test)
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
                    FamilyCode = f.FamilyCode,
                    FamilyName = f.FamilyName,
                    Address = f.Address,
                    CreatedAt = f.CreatedAt,
                    CreatedByName = f.CreatedByNavigation != null
                        ? f.CreatedByNavigation.FullName
                        : "Unknown"
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. GET: api/Families/myfamilies
        //    => Lấy tất cả gia đình mà user hiện tại tham gia HOẶC tạo ra
        // ============================================================
        [HttpGet("myfamilies")]
        [EnableQuery]
        public IQueryable<FamilyReadDTO> GetMyFamilies()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Enumerable.Empty<FamilyReadDTO>().AsQueryable();

            int userId = int.Parse(userIdClaim);

            // 🔹 1. Gia đình user đã tạo
            var createdFamilies = _context.Families
                .Where(f => f.CreatedBy == userId)
                .Select(f => new FamilyReadDTO
                {
                    FamilyId = f.FamilyId,
                    FamilyCode = f.FamilyCode,
                    FamilyName = f.FamilyName,
                    Address = f.Address,
                    CreatedAt = f.CreatedAt,
                    Relationship = "Chủ gia đình"
                });

            // 🔹 2. Gia đình user tham gia (thông qua FamilyMembers)
            var joinedFamilies = _context.FamilyMembers
                .Include(fm => fm.Family)
                .Where(fm => fm.UserId == userId)
                .Select(fm => new FamilyReadDTO
                {
                    FamilyId = fm.Family.FamilyId,
                    FamilyCode = fm.Family.FamilyCode,
                    FamilyName = fm.Family.FamilyName,
                    Address = fm.Family.Address,
                    CreatedAt = fm.Family.CreatedAt,
                    Relationship = fm.Relationship
                });

            // 🔹 3. Gộp lại và loại trùng
            return createdFamilies
                .Union(joinedFamilies)
                .GroupBy(f => f.FamilyId)
                .Select(g => g.First())
                .AsQueryable();
        }

        // ============================================================
        // 🔹 3. POST: api/Families
        //    => Tạo mới gia đình (người tạo tự động là thành viên)
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateFamily([FromBody] FamilyCreateDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            int userId = int.Parse(userIdClaim);

            if (string.IsNullOrWhiteSpace(dto.FamilyName))
                return BadRequest(new { message = "Tên gia đình không được để trống." });

            // 🔹 Sinh mã FamilyCode tự động (FAM001, FAM002, ...)
            int nextId = _context.Families.Any() ? _context.Families.Max(f => f.FamilyId) + 1 : 1;
            string nextCode = $"FAM{nextId.ToString("D3")}";

            // 🔹 Tạo mới gia đình
            var family = new Family
            {
                FamilyCode = nextCode,
                FamilyName = dto.FamilyName.Trim(),
                Address = dto.Address?.Trim(),
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
                message = "✅ Tạo gia đình thành công!",
                data = new FamilyReadDTO
                {
                    FamilyId = family.FamilyId,
                    FamilyCode = family.FamilyCode,
                    FamilyName = family.FamilyName,
                    Address = family.Address,
                    CreatedAt = family.CreatedAt,
                    CreatedByName = "Bạn",
                    Relationship = "Chủ gia đình"
                }
            });
        }

        // ============================================================
        // 🔹 4. PUT: api/Families/{id}
        //    => Cập nhật thông tin gia đình (chỉ người tạo được phép)
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamily(int id, [FromBody] FamilyUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var family = await _context.Families.FindAsync(id);
            if (family == null)
                return NotFound(new { message = "Không tìm thấy gia đình." });

            if (family.CreatedBy != userId)
                return Forbid("Bạn không có quyền sửa gia đình này.");

            if (!string.IsNullOrEmpty(dto.FamilyName))
                family.FamilyName = dto.FamilyName.Trim();

            if (!string.IsNullOrEmpty(dto.Address))
                family.Address = dto.Address.Trim();

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Cập nhật gia đình thành công!" });
        }

        // ============================================================
        // 🔹 5. DELETE: api/Families/{id}
        //    => Xóa gia đình (chỉ người tạo được xóa)
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamily(int id)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var family = await _context.Families
                .Include(f => f.FamilyMembers)
                .FirstOrDefaultAsync(f => f.FamilyId == id);

            if (family == null)
                return NotFound(new { message = "Không tìm thấy gia đình." });

            if (family.CreatedBy != userId)
                return Forbid("Bạn không có quyền xóa gia đình này.");

            _context.FamilyMembers.RemoveRange(family.FamilyMembers);
            _context.Families.Remove(family);
            await _context.SaveChangesAsync();

            return Ok(new { message = "🗑️ Xóa gia đình thành công!" });
        }
        // ============================================================
        // 🔹 6. POST: api/Families/join
        //    => Thành viên tham gia gia đình bằng mã code
        // ============================================================
        [HttpPost("join")]
        public async Task<IActionResult> JoinFamily([FromBody] JoinFamilyDTO dto)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Không xác định được người dùng." });

            int userId = int.Parse(userIdClaim);

            if (string.IsNullOrWhiteSpace(dto.FamilyCode))
                return BadRequest(new { message = "Mã gia đình không được để trống." });

            // 🔹 Tìm gia đình theo mã
            var family = await _context.Families.FirstOrDefaultAsync(f => f.FamilyCode == dto.FamilyCode.Trim());
            if (family == null)
                return NotFound(new { message = "Không tìm thấy gia đình với mã này." });

            // 🔹 Kiểm tra xem đã là thành viên chưa
            bool alreadyJoined = await _context.FamilyMembers.AnyAsync(fm => fm.FamilyId == family.FamilyId && fm.UserId == userId);
            if (alreadyJoined)
                return BadRequest(new { message = "Bạn đã là thành viên của gia đình này." });

            // 🔹 Thêm thành viên mới
            var newMember = new FamilyMember
            {
                FamilyId = family.FamilyId,
                UserId = userId,
                Relationship = "Thành viên",
                JoinDate = DateTime.Now
            };

            _context.FamilyMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"✅ Đã tham gia gia đình '{family.FamilyName}' thành công!",
                family = new { family.FamilyId, family.FamilyName, family.FamilyCode }
            });
        }

    }
}
