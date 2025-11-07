using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using Server.DTOs;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers.User
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Member,Admin")] // ✅ Cho phép cả Member và Admin
    public class ProjectsController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public ProjectsController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/Projects
        // ============================================================
        [HttpGet]
        [EnableQuery]
        public IQueryable<ProjectReadDTO> GetAll()
        {
            return _context.Projects
                .Select(p => new ProjectReadDTO
                {
                    ProjectId = p.ProjectId,
                    ProjectName = p.ProjectName,
                    Description = p.Description,
                    CreatedAt = p.CreatedAt,
                    FamilyId = p.FamilyId
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. POST: api/Projects
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ProjectCreateDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Đọc đúng claim "UserId"
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu UserId." });

            int userId = int.Parse(userIdClaim);

            var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            var project = new Project
            {
                ProjectName = req.ProjectName,
                Description = req.Description,
                CreatedAt = DateTime.Now,
                FamilyId = member.FamilyId
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Tạo dự án mới thành công.", data = project });
        }

        // ============================================================
        // 🔹 3. PUT: api/Projects/{id}
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ProjectUpdateDTO req)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Không tìm thấy dự án." });

            project.ProjectName = req.ProjectName ?? project.ProjectName;
            project.Description = req.Description ?? project.Description;

            _context.Projects.Update(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ Cập nhật dự án thành công." });
        }

        // ============================================================
        // 🔹 4. DELETE: api/Projects/{id}
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null)
                return NotFound(new { message = "Không tìm thấy dự án." });

            _context.Projects.Remove(project);
            await _context.SaveChangesAsync();

            return Ok(new { message = "🗑️ Đã xóa dự án thành công." });
        }
    }
}
