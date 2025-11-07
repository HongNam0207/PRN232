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
    [Authorize(Roles = "Member")] // 🔒 Chỉ cho phép thành viên đăng nhập
    public class CommentsController : ControllerBase
    {
        private readonly HomeTaskManagementDbContext _context;

        public CommentsController(HomeTaskManagementDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // 🔹 1. GET (OData): api/Comments/task/{taskId}
        //     => Lấy danh sách bình luận của một công việc
        // ============================================================
        [HttpGet("task/{taskId}")]
        [EnableQuery]
        public IQueryable<CommentReadDTO> GetCommentsByTask(int taskId)
        {
            return _context.Comments
                .Include(c => c.User)
                .Where(c => c.TaskId == taskId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CommentReadDTO
                {
                    CommentId = c.CommentId,
                    TaskId = c.TaskId,
                    UserId = c.UserId,
                    FullName = c.User != null ? c.User.FullName : "Người dùng không tồn tại",
                    Content = c.Content,
                    CreatedAt = c.CreatedAt,
                    ParentCommentId = c.ParentCommentId
                })
                .AsQueryable();
        }

        // ============================================================
        // 🔹 2. POST: api/Comments
        //     => Thêm mới bình luận
        // ============================================================
        [HttpPost]
        public async Task<IActionResult> CreateComment([FromBody] CommentCreateDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            // Kiểm tra task tồn tại
            var task = await _context.Tasks.FindAsync(req.TaskId);
            if (task == null)
                return NotFound(new { message = "Không tìm thấy công việc để bình luận." });

            var newComment = new Comment
            {
                TaskId = req.TaskId,
                UserId = userId,
                Content = req.Content,
                ParentCommentId = req.ParentCommentId,
                CreatedAt = DateTime.Now
            };

            _context.Comments.Add(newComment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã thêm bình luận thành công.", data = newComment });
        }

        // ============================================================
        // 🔹 3. PUT: api/Comments/{id}
        //     => Chỉnh sửa nội dung bình luận của chính mình
        // ============================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateComment(int id, [FromBody] CommentCreateDTO req)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound(new { message = "Không tìm thấy bình luận." });

            // 🔒 Chỉ cho phép sửa bình luận của chính mình
            if (comment.UserId != userId)
                return Forbid();

            comment.Content = req.Content ?? comment.Content;

            // ❌ Không có UpdatedAt — nên không cần dòng này
            // comment.UpdatedAt = DateTime.Now;

            _context.Comments.Update(comment);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Cập nhật bình luận thành công.",
                data = new
                {
                    comment.CommentId,
                    comment.Content,
                    comment.CreatedAt
                }
            });
        }

        // ============================================================
        // 🔹 4. DELETE: api/Comments/{id}
        //     => Xóa bình luận của chính mình
        // ============================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComment(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
                return NotFound(new { message = "Không tìm thấy bình luận." });

            if (comment.UserId != userId)
                return Forbid();

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa bình luận thành công." });
        }
    }
}
