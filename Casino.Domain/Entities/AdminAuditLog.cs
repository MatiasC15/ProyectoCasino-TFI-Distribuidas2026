namespace Casino.Domain.Entities;

public sealed class AdminAuditLog
{
    public Guid Id { get; set; }
    public Guid ActorUserId { get; set; }
    public Guid? TargetUserId { get; set; }
    public string Action { get; set; } = "";
    public string DetailsJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
