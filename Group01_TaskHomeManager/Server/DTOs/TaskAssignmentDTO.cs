using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    public class TaskAssignmentReadDTO
    {
        // ⚠️ Không dùng TaskAssignmentId vì model không có
        // → thay bằng TaskId + UserId làm định danh
        public int? TaskId { get; set; }
        public int? UserId { get; set; }

        public string? FullName { get; set; }

        // ⚠️ Đổi double? thành int? để trùng với EF model
        public int? ProgressPercent { get; set; }
        public DateTime? AssignedAt { get; set; }
    }

    public class TaskAssignmentCreateDTO
    {
        [Required]
        public int TaskId { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? ProgressPercent { get; set; } = 0; // ✅ đổi sang int?
    }

    public class TaskAssignmentUpdateDTO
    {
        public int? ProgressPercent { get; set; } // ✅ đổi sang int?
    }
}
