using System;
using System.ComponentModel.DataAnnotations;

namespace Server.DTOs
{
    public class ProjectReadDTO
    {
        public int ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? FamilyId { get; set; }
    }

    public class ProjectCreateDTO
    {
        [Required]
        [MaxLength(150)]
        public string ProjectName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }
    }

    public class ProjectUpdateDTO
    {
        [MaxLength(150)]
        public string? ProjectName { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}
