using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    // ============================================================
    // 🔹 Dùng khi trả về thông tin công việc
    // ============================================================
    public class TaskReadDTO
    {
        [Key]
        public int TaskId { get; set; }

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }   // Pending / Doing / Done

        public DateTime? DueDate { get; set; }

        public DateTime? CreatedAt { get; set; }

        // 🔹 Danh sách người được giao (vì 1 Task có thể nhiều người)
        public List<int?>? AssignedUserIds { get; set; }     // ✅ dùng int? để tương thích DB
        public List<string>? AssignedUserNames { get; set; } // ✅ giữ nguyên kiểu string list
    }

    // ============================================================
    // 🔹 Dùng khi tạo công việc mới
    // ============================================================
    public class TaskCreateDTO
    {
        [Required(ErrorMessage = "Tiêu đề công việc là bắt buộc.")]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        // ✅ có thể nhiều người được giao
        public List<int?>? AssignedUserIds { get; set; } = new();

        public DateTime? DueDate { get; set; }
    }

    // ============================================================
    // 🔹 Dùng khi cập nhật công việc
    // ============================================================
    public class TaskUpdateDTO
    {
        [MaxLength(150)]
        public string? Title { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        public DateTime? DueDate { get; set; }

        // ✅ cập nhật danh sách người được giao
        public List<int?>? AssignedUserIds { get; set; }
    }
}
