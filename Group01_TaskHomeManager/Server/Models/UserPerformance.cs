using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class UserPerformance
{
    public int PerformanceId { get; set; }

    public int? UserId { get; set; }

    public int? TotalTasks { get; set; }

    public int? CompletedTasks { get; set; }

    public int? OverdueTasks { get; set; }

    public double? AvgCompletionTimeHours { get; set; }

    public DateTime? CalculatedAt { get; set; }

    public virtual User? User { get; set; }
}
