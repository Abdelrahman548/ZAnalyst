namespace ZAnalyst.Domain.Entities;

public class Template: Entity
{
    public string Name { get; private set; } =  string.Empty;
    public DateTime UpdatedAt { get; private set; }

    public virtual ICollection<Button> Buttons { get; private set; } =  new List<Button>();
    public virtual ICollection<Match> Matches { get; private set; } =  new List<Match>();
    
}