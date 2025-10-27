using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class TaskAssignment
{
    public int AssignmentId { get; set; }

    public int? TaskId { get; set; }

    public int? UserId { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string? ProgressNote { get; set; }

    public int? ProgressPercent { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User? User { get; set; }
}
