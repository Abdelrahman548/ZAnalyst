namespace ZAnalyst.Desktop.ViewModels;

public abstract class WorkspacePageViewModel(string title, string description)
{
    public string Title { get; } = title;
    public string Description { get; } = description;
}
