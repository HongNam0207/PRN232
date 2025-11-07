using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    // ============================================================
    // 🔹 Dùng khi trả về bình luận
    // ============================================================
    public class CommentReadDTO
    {
        public int CommentId { get; set; }
        public int? TaskId { get; set; }
        public int? UserId { get; set; }

        public string? FullName { get; set; }       // Tên người bình luận
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }

        public int? ParentCommentId { get; set; }   // Cho phép reply comment
    }

    // ============================================================
    // 🔹 Dùng khi tạo mới bình luận
    // ============================================================
    public class CommentCreateDTO
    {
        [Required]
        public int TaskId { get; set; }

        [Required(ErrorMessage = "Nội dung bình luận là bắt buộc.")]
        [MaxLength(500)]
        public string Content { get; set; } = string.Empty;

        public int? ParentCommentId { get; set; }
    }
}
