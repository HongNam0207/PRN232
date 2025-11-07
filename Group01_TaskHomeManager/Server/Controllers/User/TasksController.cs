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
    public class TasksController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public TasksController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1️⃣ GET: api/Tasks?familyId=4 (hoặc không truyền)
        // ============================================================
        [HttpGet]
        [EnableQuery]
        public async Task<IActionResult> GetTasks([FromQuery] int? familyId)
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Thiếu thông tin người dùng." });

            int userId = int.Parse(userIdClaim);

            // ✅ Nếu client không truyền familyId => lấy family mặc định của user
            int? targetFamilyId = familyId;
            if (targetFamilyId == null)
            {
                targetFamilyId = await _context.FamilyMembers
                    .Where(m => m.UserId == userId)
                    .Select(m => m.FamilyId)
                    .FirstOrDefaultAsync();
            }

            if (targetFamilyId == null)
                return Ok(new List<TaskReadDTO>()); // ✅ Trả mảng rỗng nếu chưa có family

            // ✅ Kiểm tra quyền truy cập gia đình
            bool isMember = await _context.FamilyMembers
                .AnyAsync(m => m.FamilyId == targetFamilyId && m.UserId == userId);

            if (!isMember)
                return Forbid("Bạn không thuộc gia đình này.");

            // 🔹 Lấy danh sách công việc theo FamilyId
            var tasks = await _context.Tasks
                .Include(t => t.TaskAssignments)
                .ThenInclude(a => a.User)
                .Where(t => t.FamilyId == targetFamilyId)
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
                .ToListAsync();

            return Ok(tasks);
        }

        // ============================================================
        // 🔹 2️⃣ POST: api/Tasks
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Token không hợp lệ hoặc thiếu UserId." });

            int userId = int.Parse(userIdClaim);

            // ✅ Kiểm tra FamilyId
            if (req.FamilyId == null || req.FamilyId <= 0)
                return BadRequest(new { message = "Thiếu thông tin gia đình." });

            var familyExists = await _context.Families.AnyAsync(f => f.FamilyId == req.FamilyId);
            if (!familyExists)
                return BadRequest(new { message = "Gia đình không tồn tại." });

            var newTask = new Models.Task
            {
                Title = req.Title,
                Description = req.Description,
                Status = "Pending",
                DueDate = req.DueDate,
                CreatedAt = DateTime.Now,
                FamilyId = req.FamilyId,
                CreatedBy = userId
            };

            _context.Tasks.Add(newTask);
            await _context.SaveChangesAsync();

            // ✅ Giao việc nếu có người nhận
            if (req.AssignedUserIds != null && req.AssignedUserIds.Count > 0)
            {
                foreach (var uid in req.AssignedUserIds)
                {
                    bool inFamily = await _context.FamilyMembers.AnyAsync(f => f.FamilyId == req.FamilyId && f.UserId == uid);
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
        // 🔹 3️⃣ PUT: api/Tasks/{id}
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

            bool inFamily = await _context.FamilyMembers.AnyAsync(m => m.UserId == userId && m.FamilyId == task.FamilyId);
            if (!inFamily)
                return Forbid();

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
        // 🔹 4️⃣ DELETE: api/Tasks/{id}
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
        // 🔹 5️⃣ GET: api/Tasks/mytasks
        //     → Lấy danh sách công việc được giao cho người dùng hiện tại
        // ============================================================
        [HttpGet("mytasks")]
        public async Task<IActionResult> GetMyTasks()
        {
            var userIdClaim = User.FindFirst("UserId")?.Value;
            if (userIdClaim == null)
                return Unauthorized(new { message = "Thiếu thông tin người dùng." });

            int userId = int.Parse(userIdClaim);

            // 🔹 Lấy danh sách công việc mà user này được giao
            var assignedTasks = await _context.TaskAssignments
                .Include(a => a.Task)
                .ThenInclude(t => t.Family)
                .Where(a => a.UserId == userId)
                .Select(a => new
                {
                    a.Task.TaskId,
                    a.Task.Title,
                    a.Task.Description,
                    a.Task.Status,
                    a.Task.DueDate,
                    a.Task.CreatedAt,
                    FamilyName = a.Task.Family.FamilyName
                })
                .ToListAsync();

            // 🔹 Nếu user chưa được giao công việc nào
            if (assignedTasks == null || !assignedTasks.Any())
                return Ok(new List<object>());

            return Ok(assignedTasks);
        }

    }
}
