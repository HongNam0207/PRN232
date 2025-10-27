using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int? FamilyId { get; set; }

    public string ProjectName { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Family? Family { get; set; }

    public virtual ICollection<Task> Tasks { get; set; } = new List<Task>();
}
