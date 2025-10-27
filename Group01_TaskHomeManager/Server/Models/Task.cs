using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Task
{
    public int TaskId { get; set; }

    public int? FamilyId { get; set; }

    public int? ProjectId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Priority { get; set; }

    public string? Status { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DueDate { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();

    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual Family? Family { get; set; }

    public virtual Project? Project { get; set; }

    public virtual ICollection<TaskAssignment> TaskAssignments { get; set; } = new List<TaskAssignment>();
}
