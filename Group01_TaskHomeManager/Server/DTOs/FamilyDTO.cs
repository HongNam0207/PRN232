namespace Server.DTOs
{
    // DTO khi trả dữ liệu ra cho Client
    public class FamilyReadDTO
    {
        public int FamilyId { get; set; }
        public string FamilyName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Relationship { get; set; }
    }

    // DTO khi Client gửi dữ liệu lên (Create / Update)
    public class FamilyCreateDTO
    {
        public string FamilyName { get; set; } = string.Empty;
        public string? Address { get; set; }
    }

    public class FamilyUpdateDTO
    {
        public string? FamilyName { get; set; }
        public string? Address { get; set; }
    }
}
