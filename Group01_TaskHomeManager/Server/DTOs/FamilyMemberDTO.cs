using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    // ============================================================
    // 🔹 DTO hiển thị thông tin thành viên trong gia đình
    // ============================================================
    public class FamilyMemberReadDTO
    {
        [Key]
        public int MemberId { get; set; }

        public int? UserId { get; set; }                 // Có thể null nếu chưa gắn user

        [MaxLength(100)]
        public string? FullName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Relationship { get; set; }

        public DateTime? JoinDate { get; set; }          // Cho phép null (vì trong DB có thể chưa gán)
    }

    // ============================================================
    // 🔹 DTO tạo mới thành viên (Client gửi lên)
    // ============================================================
    public class FamilyMemberCreateDTO
    {
        [Required(ErrorMessage = "UserId là bắt buộc.")]
        public int UserId { get; set; }

        [MaxLength(50)]
        public string? Relationship { get; set; }
    }

    // ============================================================
    // 🔹 DTO cập nhật quan hệ thành viên
    // ============================================================
    public class FamilyMemberUpdateDTO
    {
        [MaxLength(50)]
        public string? Relationship { get; set; }
    }
}
