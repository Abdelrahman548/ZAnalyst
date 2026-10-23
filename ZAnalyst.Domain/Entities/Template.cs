namespace ZAnalyst.Domain.Entities;

public class Template: Entity
{
    public string Name { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public virtual ICollection<Button> Buttons { get; private set; } =  new List<Button>();
    public virtual ICollection<Match> Matches { get; private set; } =  new List<Match>();

    private Template()
    {
        Name = string.Empty;
    }

    public Template(string name)
    {
        Name = string.Empty;
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));

        Name = name.Trim();
        UpdatedAt = DateTime.Now;
    }
}