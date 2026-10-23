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

    private Match()
    {
    }

    public Match(Guid teamId, Guid templateId, string? videoPath = null, string? videoSha256 = null)
    {
        if (teamId == Guid.Empty)
            throw new ArgumentException("Team id cannot be empty.", nameof(teamId));
        if (templateId == Guid.Empty)
            throw new ArgumentException("Template id cannot be empty.", nameof(templateId));

        TeamId = teamId;
        TemplateId = templateId;
        VideoPath = videoPath;
        VideoSha256 = videoSha256;
        CreatedAt = DateTime.Now;
    }

    public void UpdateVideo(string? videoPath, string? videoSha256)
    {
        VideoPath = videoPath;
        VideoSha256 = videoSha256;
    }
}