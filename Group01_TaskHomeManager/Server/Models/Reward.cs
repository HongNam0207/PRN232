using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Reward
{
    public int RewardId { get; set; }

    public int? UserId { get; set; }

    public int? Points { get; set; }

    public string? Badge { get; set; }

    public DateTime? EarnedAt { get; set; }

    public string? Description { get; set; }

    public virtual User? User { get; set; }
}
