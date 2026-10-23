using Avalonia.Controls;

namespace ZAnalyst.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(ViewModels.ShellViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
    }
}