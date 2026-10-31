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
        ValidateName(name);
        if (mode == PressMode.Single)
            ValidatePressDurations(preSec, postSec);
        Name = name.Trim();
        Color = color;
        Type = type;
        Mode = mode;
        HotKey = hotkey;
        Note = note;
        PreSec = mode == PressMode.Single ? preSec : 0;
        PostSec = mode == PressMode.Single ? postSec : 0;
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

    public void ToSinglePressMode(float preSec = 0, float postSec = 0)
    {
        ValidatePressDurations(preSec, postSec);
        Mode = PressMode.Single;
        PreSec = preSec;
        PostSec = postSec;
        UpdatedAt = DateTime.Now;
    }

    public void ToDoublePressMode()
    {
        Mode = PressMode.Double;
        PreSec = 0;
        PostSec = 0;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateColor(Color color)
    {
        Color = color;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateName(string name)
    {
        ValidateName(name);
        Name = name.Trim();
        UpdatedAt = DateTime.Now;
    }
    public void UpdateNote(string note)
    {
        Note = note;
        UpdatedAt = DateTime.Now;
    }

    public void UpdateConfiguration(
        string name,
        Color color,
        ButtonType type,
        PressMode mode,
        string? hotkey,
        string? note,
        float preSec,
        float postSec)
    {
        ValidateName(name);
        if (mode == PressMode.Single)
            ValidatePressDurations(preSec, postSec);

        Name = name.Trim();
        Color = color;
        Type = type;
        Mode = mode;
        HotKey = hotkey;
        Note = note;
        PreSec = mode == PressMode.Single ? preSec : 0;
        PostSec = mode == PressMode.Single ? postSec : 0;
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

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be null or whitespace.", nameof(name));
    }

    private static void ValidatePressDurations(float preSec, float postSec)
    {
        if (!float.IsFinite(preSec) || preSec < 0)
            throw new ArgumentOutOfRangeException(nameof(preSec), "Duration must be finite and non-negative.");
        if (!float.IsFinite(postSec) || postSec < 0)
            throw new ArgumentOutOfRangeException(nameof(postSec), "Duration must be finite and non-negative.");
    }
}