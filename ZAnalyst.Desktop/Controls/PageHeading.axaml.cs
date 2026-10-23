using Avalonia;
using Avalonia.Controls;

namespace ZAnalyst.Desktop.Controls;

public partial class PageHeading : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<PageHeading, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<PageHeading, string>(nameof(Description), string.Empty);

    public PageHeading()
    {
        InitializeComponent();
        UpdateText();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TitleProperty || change.Property == DescriptionProperty)
            UpdateText();
    }

    private void UpdateText()
    {
        if (TitleText is not null)
            TitleText.Text = Title;
        if (DescriptionText is not null)
            DescriptionText.Text = Description;
    }
}
