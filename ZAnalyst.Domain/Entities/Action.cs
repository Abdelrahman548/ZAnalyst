using ZAnalyst.Domain.Enums;

namespace ZAnalyst.Domain.Entities;

public class Action: Entity
{
    public Guid MatchId { get; private set; }
    public Guid ButtonId { get; private set; }
    public Guid PlayerId { get; private set; }
    public ActionRate Rate { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public virtual Match? Match { get; private set; }
    public virtual Button? Button { get; private set; }
    public virtual Player? Player { get; private set; }
    
    protected Action()
    {
        
    }

    public Action(Guid matchId, Guid buttonId, Guid playerId, ActionRate rate, DateTime startTime, DateTime endTime, string? note)
    {
        if(startTime > endTime)
            throw new ArgumentException("Start time must be before end time.");
        if(matchId == Guid.Empty)
            throw new ArgumentException("Match id cannot be empty.");
        if(buttonId == Guid.Empty)
            throw new ArgumentException("Button id cannot be empty.");
        
        MatchId = matchId;
        ButtonId = buttonId;
        PlayerId = playerId;
        Rate = rate;
        StartTime = startTime; 
        EndTime = endTime;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
    }
    
    public void UpdateTime(DateTime start, DateTime end)
    {
        if(start > end)
            throw new ArgumentException("Start time must be before end time.");
        StartTime = start;
        EndTime = end;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateNote(string note)
    {
        Note = note;
        UpdatedAt = DateTime.Now;
    }
    
    public void UpdatePlayer(Guid playerId)
    {
        PlayerId = playerId;
        UpdatedAt = DateTime.Now;
    }
    
    public void UpdateRate(ActionRate rate)
    {
        Rate = rate;
        UpdatedAt = DateTime.Now;
    }
}