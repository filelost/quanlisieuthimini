namespace QuanLySieuThi.Models.Entities;

public sealed class AuditLog
{
    public long AuditLogId { get; set; }
    public int? ActorUserId { get; set; }
    public string ActorUsername { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
