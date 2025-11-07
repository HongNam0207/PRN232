namespace Server.DTOs
{
    // DTO khi trả dữ liệu ra cho Client
    public class FamilyReadDTO
    {
        public int FamilyId { get; set; }
        public string FamilyCode { get; set; } = string.Empty; // ✅ Mã code (FAM001, FAM002...)
        public string FamilyName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Relationship { get; set; }
    }

    // DTO khi Client gửi dữ liệu lên (Create)
    public class FamilyCreateDTO
    {
        public string FamilyName { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    // DTO khi cập nhật thông tin gia đình
    public class FamilyUpdateDTO
    {
        public string? FamilyName { get; set; }
        public string? Address { get; set; }
    }
    
        public class JoinFamilyDTO
        {
            public string FamilyCode { get; set; } = string.Empty;
        }
    

}
