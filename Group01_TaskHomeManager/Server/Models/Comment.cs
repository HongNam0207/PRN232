using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int? TaskId { get; set; }

    public int? UserId { get; set; }

    public string? Content { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? ParentCommentId { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User? User { get; set; }
}
