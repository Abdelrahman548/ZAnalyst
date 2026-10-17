using System.Drawing;
using ZAnalyst.Domain.Enums;

namespace ZAnalyst.Domain.Entities;

public class Button: Entity
{
    public Guid TemplateId { get; private set; }
    public string? Name { get; private set; }
    public Color Color { get; private set; }
    public ButtonType Type{ get; private set; }
    public PressMode Mode { get; private set; }
    public float PreSec { get; private set; } = 0;
    public float PostSec { get; private set; } = 0;
    public string? Note { get; private set; }
    public string? HotKey { get; set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public bool Archived { get; private set; }
    
    protected Button()
    {
        
    }

    public Button(Guid templateId, string name, Color color, ButtonType type, PressMode mode,string? hotkey = null, string? note= null, float preSec = 0, float postSec = 0)
    {
        if(templateId == Guid.Empty)
            throw new ArgumentException("Template id cannot be empty.");
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Color = color;
        Type = type;
        Mode = mode;
        HotKey = hotkey;
        Note = note;
        PreSec = preSec;
        PostSec = postSec;
        TemplateId = templateId;
        CreatedAt = DateTime.Now;
        UpdatedAt = DateTime.Now;
        Archived = false;
    }
    
    public void UpdateKey(string key)
    {
        HotKey = key;
        UpdatedAt = DateTime.Now;
    }

    public void ToSinglePressMode()
    {
        Mode = PressMode.Single;
        UpdatedAt = DateTime.Now;
    }

    public void ToDoublePressMode(float preSec, float postSec)
    {
        PreSec = preSec;
        PostSec = postSec;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateColor(Color color)
    {
        Color = color;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateName(string name)
    {
        Name = name;
        UpdatedAt = DateTime.Now;
    }
    public void UpdateNote(string note)
    {
        Note = note;
        UpdatedAt = DateTime.Now;
    }

    public void Archive()
    {
        Archived = true;
        UpdatedAt = DateTime.Now;
        
    }

    public void UnArchive()
    {
        Archived = false;
        UpdatedAt = DateTime.Now;
    }
}