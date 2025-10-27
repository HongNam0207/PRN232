using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class FamilyMember
{
    public int MemberId { get; set; }

    public int? FamilyId { get; set; }

    public int? UserId { get; set; }

    public string? Relationship { get; set; }

    public DateTime? JoinDate { get; set; }

    public virtual Family? Family { get; set; }

    public virtual User? User { get; set; }
}
