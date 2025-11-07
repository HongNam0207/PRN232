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
    [Authorize(Roles = "Member")] // ✅ Cho phép cả Member và Admin
    public class TasksController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public TasksController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/Tasks
        // ============================================================
        [HttpGet]
        [EnableQuery]
        public IQueryable<TaskReadDTO> GetTasks()
        {
            return _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
                .Select(t => new TaskReadDTO
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    AssignedUserIds = t.TaskAssignments.Select(a => a.UserId).ToList(),
                    AssignedUserNames = t.TaskAssignments.Select(a => a.User.FullName).ToList()
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. GET: api/Tasks/{id}
        // ============================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaskById(int id)
        {
            var task = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.TaskId == id);

            if (task == null)
                return NotFound(new { message = "Không tìm thấy công việc." });

            var dto = new TaskReadDTO
            {
                TaskId = task.TaskId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                DueDate = task.DueDate,
                CreatedAt = task.CreatedAt,
                AssignedUserIds = task.TaskAssignments.Select(a => a.UserId).ToList(),
                AssignedUserNames = task.TaskAssignments.Select(a => a.User.FullName).ToList()
            };

            return Ok(dto);
        }

        // ============================================================
        // 🔹 3. POST: api/Tasks
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // ✅ Đọc claim theo key "UserId" đúng với token bạn tạo
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu UserId." });

            int userId = int.Parse(userIdClaim);

            var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            int familyId = member.FamilyId ?? 0;

            var newTask = new Models.Task
            {
                Title = req.Title,
                Description = req.Description,
                Status = "Pending",
                DueDate = req.DueDate,
                CreatedAt = DateTime.Now,
                FamilyId = familyId,
                CreatedBy = userId
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            // Giao việc nếu có người nhận
            if (req.AssignedUserIds != null && req.AssignedUserIds.Count > 0)
            {
                foreach (var uid in req.AssignedUserIds)
                {
                    bool inFamily = await _context.FamilyMembers.AnyAsync(f => f.FamilyId == familyId && f.UserId == uid);
                    if (!inFamily) continue;

                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = newTask.TaskId,
                        UserId = uid,
                        AssignedAt = DateTime.Now,
                        ProgressPercent = 0
                    });
                }

                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "✅ Tạo công việc mới thành công.", data = newTask });
        }

        // ============================================================
        // 🔹 4. PUT: api/Tasks/{id}
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, [FromBody] TaskUpdateDTO req)
        {
            var task = await _context.Tasks.Include(t => t.TaskAssignments).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null)
                return NotFound(new { message = "Không tìm thấy công việc." });

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);
            var member = await _context.FamilyMembers.FirstOrDefaultAsync(m => m.UserId == userId);
            if (member == null)
                return BadRequest(new { message = "Bạn chưa thuộc về gia đình nào." });

            if ((task.FamilyId ?? 0) != (member.FamilyId ?? 0))
                return Forbid();

            // Cập nhật thông tin
            task.Title = req.Title ?? task.Title;
            task.Description = req.Description ?? task.Description;
            task.Status = req.Status ?? task.Status;
            task.DueDate = req.DueDate ?? task.DueDate;
            task.UpdatedAt = DateTime.Now;

            if (req.AssignedUserIds != null)
            {
                _context.TaskAssignments.RemoveRange(task.TaskAssignments);
                foreach (var uid in req.AssignedUserIds)
                {
                    _context.TaskAssignments.Add(new TaskAssignment
                    {
                        TaskId = task.TaskId,
                        UserId = uid,
                        AssignedAt = DateTime.Now,
                        ProgressPercent = 0
                    });
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "✅ Cập nhật công việc thành công." });
        }

        // ============================================================
        // 🔹 5. DELETE: api/Tasks/{id}
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.Include(t => t.TaskAssignments).FirstOrDefaultAsync(t => t.TaskId == id);
            if (task == null)
                return NotFound(new { message = "Không tìm thấy công việc." });

            _context.TaskAssignments.RemoveRange(task.TaskAssignments);
            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();

            return Ok(new { message = "🗑️ Đã xóa công việc." });
        }

        // ============================================================
        // 🔹 6. GET: api/Tasks/family
        // ============================================================
        [HttpGet("family")]
        [EnableQuery]
        public IQueryable<TaskReadDTO> GetTasksInMyFamily()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Enumerable.Empty<TaskReadDTO>().AsQueryable();

            int userId = int.Parse(userIdClaim);
            var familyId = _context.FamilyMembers.Where(m => m.UserId == userId).Select(m => m.FamilyId).FirstOrDefault();

            return _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
                .Where(t => t.FamilyId == familyId)
                .Select(t => new TaskReadDTO
                {
                    TaskId = t.TaskId,
                    Title = t.Title,
                    Description = t.Description,
                    Status = t.Status,
                    DueDate = t.DueDate,
                    CreatedAt = t.CreatedAt,
                    AssignedUserIds = t.TaskAssignments.Select(a => a.UserId).ToList(),
                    AssignedUserNames = t.TaskAssignments.Select(a => a.User.FullName).ToList()
                })
                .AsQueryable();
        }
    }
}
