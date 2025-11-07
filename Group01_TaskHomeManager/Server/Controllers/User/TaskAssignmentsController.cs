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
    [Authorize(Roles = "Member")]
    public class TaskAssignmentsController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public TaskAssignmentsController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/TaskAssignments
        // ============================================================
        [HttpGet]
        [EnableQuery]
        public IQueryable<TaskAssignmentReadDTO> GetAll()
        {
            return _context.TaskAssignments
                .Include(a => a.User)
                .Include(a => a.Task)
                .Select(a => new TaskAssignmentReadDTO
                {
                    TaskId = a.TaskId,
                    UserId = a.UserId,
                    FullName = a.User != null ? a.User.FullName : "Người dùng không tồn tại",
                    ProgressPercent = a.ProgressPercent,
                    AssignedAt = a.AssignedAt
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. POST: api/TaskAssignments
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] TaskAssignmentCreateDTO req)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null) return Unauthorized();

            // Kiểm tra Task tồn tại
            var task = await _context.Tasks.FindAsync(req.TaskId);
            if (task == null)
                return NotFound(new { message = "Không tìm thấy công việc." });

            // Kiểm tra user tồn tại
            var user = await _context.Users.FindAsync(req.UserId);
            if (user == null)
                return NotFound(new { message = "Không tìm thấy người dùng." });

            // Kiểm tra trùng
            bool exists = await _context.TaskAssignments.AnyAsync(a => a.TaskId == req.TaskId && a.UserId == req.UserId);
            if (exists)
                return Conflict(new { message = "Người này đã được giao công việc này." });

            var newAssignment = new TaskAssignment
            {
                TaskId = req.TaskId,
                UserId = req.UserId,
                ProgressPercent = req.ProgressPercent,
                AssignedAt = DateTime.Now
            };

            _context.TaskAssignments.Add(newAssignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã giao việc thành công." });
        }

        // ============================================================
        // 🔹 3. PUT: api/TaskAssignments/{taskId}/{userId}
        // ============================================================
        [HttpPut("{taskId}/{userId}")]
        public async Task<IActionResult> UpdateProgress(int taskId, int userId, [FromBody] TaskAssignmentUpdateDTO req)
        {
            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId);

            if (assignment == null)
                return NotFound(new { message = "Không tìm thấy bản ghi giao việc." });

            assignment.ProgressPercent = req.ProgressPercent ?? assignment.ProgressPercent;
            _context.TaskAssignments.Update(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cập nhật tiến độ thành công." });
        }

        // ============================================================
        // 🔹 4. DELETE: api/TaskAssignments/{taskId}/{userId}
        // ============================================================
        [HttpDelete("{taskId}/{userId}")]
        public async Task<IActionResult> DeleteAssignment(int taskId, int userId)
        {
            var assignment = await _context.TaskAssignments
                .FirstOrDefaultAsync(a => a.TaskId == taskId && a.UserId == userId);

            if (assignment == null)
                return NotFound(new { message = "Không tìm thấy bản ghi giao việc." });

            _context.TaskAssignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã gỡ người này khỏi công việc." });
        }
    }
}
