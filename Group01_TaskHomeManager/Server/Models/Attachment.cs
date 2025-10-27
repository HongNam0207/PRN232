using System;
using System.Collections.Generic;

namespace Server.Models;

public partial class Attachment
{
    public int AttachmentId { get; set; }

    public int? TaskId { get; set; }

    public string FileUrl { get; set; } = null!;

    public int? UploadedBy { get; set; }

    public DateTime? UploadedAt { get; set; }

    public string? FileType { get; set; }

    public virtual Task? Task { get; set; }

    public virtual User? UploadedByNavigation { get; set; }
}
