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
    public class FamilyMembersController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public FamilyMembersController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/FamilyMembers/myfamily
        // ============================================================
        [HttpGet("myfamily")]
        [EnableQuery]
        public IQueryable<FamilyMemberReadDTO> GetMyFamilyMembers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Enumerable.Empty<FamilyMemberReadDTO>().AsQueryable();

            int userId = int.Parse(userIdClaim);

            var myFamily = _context.FamilyMembers
                .FirstOrDefault(fm => fm.UserId == userId);

            if (myFamily == null)
                return Enumerable.Empty<FamilyMemberReadDTO>().AsQueryable();

            int familyId = myFamily.FamilyId ?? 0;

            return _context.FamilyMembers
                .Include(fm => fm.User)
                .Where(fm => fm.FamilyId == familyId)
                .Select(fm => new FamilyMemberReadDTO
                {
                    MemberId = fm.MemberId,
                    UserId = fm.UserId,
                    FullName = fm.User.FullName,
                    Email = fm.User.Email,
                    Relationship = fm.Relationship,
                    JoinDate = fm.JoinDate
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. POST: api/FamilyMembers/add
        // ============================================================
        [HttpPost("add")]
        public async Task<IActionResult> AddMemberToMyFamily([FromBody] FamilyMemberCreateDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var myFamilyMember = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (myFamilyMember == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            int familyId = myFamilyMember.FamilyId ?? 0;

            var targetUser = await _context.Users.FindAsync(dto.UserId);
            if (targetUser == null)
                return NotFound(new { message = "Không tìm thấy người dùng cần thêm." });

            bool exists = await _context.FamilyMembers.AnyAsync(m => m.UserId == dto.UserId && m.FamilyId == familyId);
            if (exists)
                return Conflict(new { message = "Người này đã là thành viên trong gia đình." });

            var newMember = new FamilyMember
            {
                FamilyId = familyId,
                UserId = dto.UserId,
                Relationship = dto.Relationship ?? "Thành viên",
                JoinDate = DateTime.Now
            };

            _context.FamilyMembers.Add(newMember);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm thành viên vào gia đình của bạn." });
        }

        // ============================================================
        // 🔹 3. PUT: api/FamilyMembers/{memberId}
        // ============================================================
        [HttpPut("{memberId}")]
        public async Task<IActionResult> UpdateRelationship(int memberId, [FromBody] FamilyMemberUpdateDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var myFamilyMember = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (myFamilyMember == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            var member = await _context.FamilyMembers.FindAsync(memberId);
            if (member == null)
                return NotFound(new { message = "Không tìm thấy thành viên." });

            if ((member.FamilyId ?? 0) != (myFamilyMember.FamilyId ?? 0))
                return Forbid();

            member.Relationship = dto.Relationship ?? member.Relationship;
            _context.FamilyMembers.Update(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật thông tin thành công." });
        }

        // ============================================================
        // 🔹 4. DELETE: api/FamilyMembers/{memberId}
        // ============================================================
        [HttpDelete("{memberId}")]
        public async Task<IActionResult> RemoveMember(int memberId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var myFamilyMember = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (myFamilyMember == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            var member = await _context.FamilyMembers.FindAsync(memberId);
            if (member == null)
                return NotFound(new { message = "Không tìm thấy thành viên." });

            if ((member.FamilyId ?? 0) != (myFamilyMember.FamilyId ?? 0))
                return Forbid();

            _context.FamilyMembers.Remove(member);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa thành viên khỏi gia đình." });
        }

        // ============================================================
        // 🔹 5. GET: api/FamilyMembers/myfamilyinfo
        // ============================================================
        [HttpGet("myfamilyinfo")]
        [EnableQuery]
        public IQueryable<object> GetMyFamilyInfo()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Enumerable.Empty<object>().AsQueryable();

            int userId = int.Parse(userIdClaim);

            return _context.FamilyMembers
                .Include(fm => fm.Family)
                .Where(fm => fm.UserId == userId)
                .Select(fm => new
                {
                    FamilyId = fm.FamilyId ?? 0,
                    fm.Family.FamilyName,
                    fm.Family.Address,
                    fm.Relationship,
                    fm.JoinDate
                })
                .AsQueryable();
        }
    }
}
