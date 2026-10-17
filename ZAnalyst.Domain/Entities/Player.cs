namespace ZAnalyst.Domain.Entities;

public class Player: Entity
{
    public string Name { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public Guid TeamId { get; private set; }
    public virtual ICollection<Action> Actions { get; private set; } =  new List<Action>();
    public virtual Team?  Team { get; private set; }

    protected Player()
    {
        
    }

    public Player(string name, int number, Guid teamId)
    {
        Update(name, number, teamId);
    }

    public void Update(string name, int number, Guid teamId)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty.", nameof(name));
        if(number < 0)
            throw new ArgumentException("Number cannot be negative.", nameof(number));
        Name = name;
        Number = number;
        TeamId = teamId;
    }
}