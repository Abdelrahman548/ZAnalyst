namespace ZAnalyst.Domain.Entities;

public class Match: Entity
{
    public Guid TeamId { get; private set; }
    public Guid TemplateId { get; private set; }
    public string? VideoSha256 { get; private set; }
    public string? VideoPath { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public virtual Team? Team { get; private set; }
    public virtual Template? Template { get; private set; }
    
    
}