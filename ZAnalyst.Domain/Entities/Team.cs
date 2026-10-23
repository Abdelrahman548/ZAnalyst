namespace ZAnalyst.Domain.Entities;

public class Team: Entity
{
    public string Name { get; private set; }
    public ICollection<Player> Players { get; private set; } =  new List<Player>();
    public ICollection<Match>  Matches { get; private set; } =  new List<Match>();

    private Team()
    {
        Name = string.Empty;
    }

    public Team(string name)
    {
        Name = string.Empty;
        UpdateName(name);
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
    }
}