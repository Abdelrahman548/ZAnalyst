namespace ZAnalyst.Domain.Entities;

public class Team: Entity
{
    public string Name { get; private set; } = string.Empty;
    public ICollection<Player> Players { get; private set; } =  new List<Player>();
    public ICollection<Match>  Matches { get; private set; } =  new List<Match>();
    
}