using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ZAnalyst.Application.Interfaces;
using ZAnalyst.Domain.Entities;
using ZAnalyst.Domain.Enums;

namespace ZAnalyst.Desktop.ViewModels;

public sealed class TemplatesViewModel : WorkspacePageViewModel, INotifyPropertyChanged
{
    private readonly ITemplateService _templateService;
    private readonly IButtonService _buttonService;
    private Template? _selectedTemplate;
    private Button? _selectedButton;
    private string _newTemplateName = string.Empty;
    private string _editTemplateName = string.Empty;
    private string _buttonName = string.Empty;
    private string _buttonColor = "#55B7A8";
    private ButtonType _buttonType = ButtonType.Event;
    private PressMode _pressMode = PressMode.Single;
    private string _hotkey = string.Empty;
    private string _note = string.Empty;
    private string _preSeconds = "0";
    private string _postSeconds = "0";
    private string? _errorMessage;
    private bool _isBusy;
    private bool _isReloadingTemplates;

    public TemplatesViewModel(ITemplateService templateService, IButtonService buttonService)
        : base("Templates", "Configure reusable event and action controls.")
    {
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _buttonService = buttonService ?? throw new ArgumentNullException(nameof(buttonService));

        CreateTemplateCommand = new AsyncCommand(CreateTemplateAsync, () => !IsBusy && !string.IsNullOrWhiteSpace(NewTemplateName));
        UpdateTemplateCommand = new AsyncCommand(UpdateTemplateAsync, () => !IsBusy && SelectedTemplate is not null && !string.IsNullOrWhiteSpace(EditTemplateName));
        DeleteTemplateCommand = new AsyncCommand(DeleteTemplateAsync, () => !IsBusy && SelectedTemplate is not null);
        RefreshCommand = new AsyncCommand(LoadTemplatesAsync, () => !IsBusy);
        CreateButtonCommand = new AsyncCommand(CreateButtonAsync, CanCreateButton);
        UpdateButtonCommand = new AsyncCommand(UpdateButtonAsync, CanUpdateButton);
        NewButtonCommand = new RelayCommand(_ => SelectedButton = null, _ => !IsBusy && SelectedTemplate is not null);
        ArchiveButtonCommand = new AsyncCommand(ArchiveButtonAsync, () => !IsBusy && SelectedButton is { Archived: false });
        UnarchiveButtonCommand = new AsyncCommand(UnarchiveButtonAsync, () => !IsBusy && SelectedButton is { Archived: true });
        DeleteButtonCommand = new AsyncCommand(DeleteButtonAsync, () => !IsBusy && SelectedButton is not null);
        SelectButtonCommand = new RelayCommand(SelectButton, parameter => parameter is Button);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Template> Templates { get; } = [];
    public ObservableCollection<Button> Buttons { get; } = [];
    public IReadOnlyList<ButtonType> ButtonTypes { get; } = Enum.GetValues<ButtonType>();
    public IReadOnlyList<PressMode> PressModes { get; } = Enum.GetValues<PressMode>();

    public ICommand CreateTemplateCommand { get; }
    public ICommand UpdateTemplateCommand { get; }
    public ICommand DeleteTemplateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CreateButtonCommand { get; }
    public ICommand UpdateButtonCommand { get; }
    public ICommand NewButtonCommand { get; }
    public ICommand ArchiveButtonCommand { get; }
    public ICommand UnarchiveButtonCommand { get; }
    public ICommand DeleteButtonCommand { get; }
    public ICommand SelectButtonCommand { get; }

    public Template? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (ReferenceEquals(_selectedTemplate, value))
                return;

            _selectedTemplate = value;
            EditTemplateName = value?.Name ?? string.Empty;
            SelectedButton = null;
            ResetButtonEditor();
            Buttons.Clear();
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedTemplate));
            OnPropertyChanged(nameof(IsCreatingButton));
            OnPropertyChanged(nameof(HasButtons));
            OnPropertyChanged(nameof(HasNoButtons));
            ClearError();
            NotifyCommandStates();

            if (value is not null && !_isReloadingTemplates)
                _ = LoadButtonsAsync(value.Id);
        }
    }

    public Button? SelectedButton
    {
        get => _selectedButton;
        private set
        {
            if (ReferenceEquals(_selectedButton, value))
                return;

            _selectedButton = value;
            if (value is null)
                ResetButtonEditor();
            else
                PopulateButtonEditor(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedButton));
            OnPropertyChanged(nameof(IsCreatingButton));
            OnPropertyChanged(nameof(HasArchivedSelectedButton));
            OnPropertyChanged(nameof(HasArchivableSelectedButton));
            OnPropertyChanged(nameof(IsDoublePressMode));
            OnPropertyChanged(nameof(IsSinglePressMode));
            NotifyCommandStates();
        }
    }

    public string NewTemplateName
    {
        get => _newTemplateName;
        set
        {
            if (SetProperty(ref _newTemplateName, value))
                NotifyCommandStates();
        }
    }

    public string EditTemplateName
    {
        get => _editTemplateName;
        set
        {
            if (SetProperty(ref _editTemplateName, value))
                NotifyCommandStates();
        }
    }

    public string ButtonName
    {
        get => _buttonName;
        set
        {
            if (SetProperty(ref _buttonName, value))
                NotifyCommandStates();
        }
    }

    public string ButtonColor
    {
        get => _buttonColor;
        set
        {
            if (SetProperty(ref _buttonColor, value))
                NotifyCommandStates();
        }
    }

    public ButtonType SelectedButtonType
    {
        get => _buttonType;
        set
        {
            if (SetProperty(ref _buttonType, value))
                NotifyCommandStates();
        }
    }

    public PressMode SelectedPressMode
    {
        get => _pressMode;
        set
        {
            if (SetProperty(ref _pressMode, value))
            {
                OnPropertyChanged(nameof(IsDoublePressMode));
                OnPropertyChanged(nameof(IsSinglePressMode));
                NotifyCommandStates();
            }
        }
    }

    public string Hotkey
    {
        get => _hotkey;
        set
        {
            if (SetProperty(ref _hotkey, value))
                NotifyCommandStates();
        }
    }

    public string Note
    {
        get => _note;
        set
        {
            if (SetProperty(ref _note, value))
                NotifyCommandStates();
        }
    }

    public string PreSeconds
    {
        get => _preSeconds;
        set
        {
            if (SetProperty(ref _preSeconds, value))
                NotifyCommandStates();
        }
    }

    public string PostSeconds
    {
        get => _postSeconds;
        set
        {
            if (SetProperty(ref _postSeconds, value))
                NotifyCommandStates();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                NotifyCommandStates();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelectedTemplate => SelectedTemplate is not null;
    public bool HasSelectedButton => SelectedButton is not null;
    public bool IsCreatingButton => SelectedTemplate is not null && SelectedButton is null;
    public bool HasArchivedSelectedButton => SelectedButton is { Archived: true };
    public bool HasArchivableSelectedButton => SelectedButton is { Archived: false };
    public bool HasTemplates => Templates.Count > 0;
    public bool HasNoTemplates => Templates.Count == 0;
    public bool HasButtons => Buttons.Count > 0;
    public bool HasNoButtons => Buttons.Count == 0;
    public bool IsDoublePressMode => SelectedPressMode == PressMode.Double;
    public bool IsSinglePressMode => SelectedPressMode == PressMode.Single;

    public Task InitializeAsync() => LoadTemplatesAsync();

    private async Task LoadTemplatesAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var selectedId = SelectedTemplate?.Id;
            var templates = await _templateService.GetAllAsync();
            Templates.Clear();
            foreach (var template in templates)
                Templates.Add(template);
            NotifyTemplatesChanged();

            _isReloadingTemplates = true;
            SelectedTemplate = Templates.FirstOrDefault(template => template.Id == selectedId)
                ?? Templates.FirstOrDefault();
            _isReloadingTemplates = false;
            if (SelectedTemplate is { } selected)
                await LoadButtonsAsync(selected.Id);
        }
        catch (Exception exception)
        {
            _isReloadingTemplates = false;
            ErrorMessage = $"Could not load templates: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CreateTemplateAsync()
    {
        IsBusy = true;
        ClearError();
        try
        {
            var template = await _templateService.CreateAsync(new Template(NewTemplateName));
            Templates.Add(template);
            NotifyTemplatesChanged();
            NewTemplateName = string.Empty;
            _isReloadingTemplates = true;
            SelectedTemplate = template;
            _isReloadingTemplates = false;
            await LoadButtonsAsync(template.Id);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not create template: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateTemplateAsync()
    {
        var template = SelectedTemplate;
        if (template is null)
            return;

        IsBusy = true;
        ClearError();
        var oldName = template.Name;
        try
        {
            template.Rename(EditTemplateName);
            await _templateService.UpdateAsync(template.Id, template);
            RefreshCollectionItem(Templates, template);
            EditTemplateName = template.Name;
        }
        catch (Exception exception)
        {
            template.Rename(oldName);
            RefreshCollectionItem(Templates, template);
            EditTemplateName = oldName;
            ErrorMessage = $"Could not update template: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DeleteTemplateAsync()
    {
        var template = SelectedTemplate;
        if (template is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _templateService.RemoveAsync(template.Id);
            Templates.Remove(template);
            NotifyTemplatesChanged();
            _isReloadingTemplates = true;
            SelectedTemplate = Templates.FirstOrDefault();
            _isReloadingTemplates = false;
            if (SelectedTemplate is { } next)
                await LoadButtonsAsync(next.Id);
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete template: {exception.Message}";
        }
        finally
        {
            _isReloadingTemplates = false;
            IsBusy = false;
        }
    }

    private async Task LoadButtonsAsync(Guid templateId)
    {
        var wasBusy = IsBusy;
        if (!wasBusy)
            IsBusy = true;
        ClearError();
        try
        {
            var buttons = await _buttonService.GetTemplateButtonsAsync(templateId, includeArchived: true);
            if (SelectedTemplate?.Id != templateId)
                return;

            Buttons.Clear();
            foreach (var button in buttons)
                Buttons.Add(button);
            NotifyButtonsChanged();
        }
        catch (Exception exception)
        {
            if (SelectedTemplate?.Id == templateId)
                ErrorMessage = $"Could not load buttons: {exception.Message}";
        }
        finally
        {
            if (!wasBusy)
                IsBusy = false;
        }
    }

    private async Task CreateButtonAsync()
    {
        var template = SelectedTemplate;
        if (template is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            var (color, preSeconds, postSeconds) = ReadButtonConfiguration();
            var button = new Button(
                template.Id,
                ButtonName,
                color,
                SelectedButtonType,
                SelectedPressMode,
                NormalizeOptional(Hotkey),
                NormalizeOptional(Note),
                preSeconds,
                postSeconds);
            await _buttonService.CreateAsync(button);
            Buttons.Add(button);
            NotifyButtonsChanged();
            SelectedButton = button;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not create button: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateButtonAsync()
    {
        var button = SelectedButton;
        if (button is null || button.Archived)
            return;

        IsBusy = true;
        ClearError();
        var oldConfiguration = CaptureConfiguration(button);
        try
        {
            var (color, preSeconds, postSeconds) = ReadButtonConfiguration();
            button.UpdateConfiguration(
                ButtonName,
                color,
                SelectedButtonType,
                SelectedPressMode,
                NormalizeOptional(Hotkey),
                NormalizeOptional(Note),
                preSeconds,
                postSeconds);
            await _buttonService.UpdateAsync(button.Id, button);
            RefreshCollectionItem(Buttons, button);
            PopulateButtonEditor(button);
        }
        catch (Exception exception)
        {
            RestoreConfiguration(button, oldConfiguration);
            RefreshCollectionItem(Buttons, button);
            PopulateButtonEditor(button);
            ErrorMessage = $"Could not update button: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ArchiveButtonAsync()
    {
        var button = SelectedButton;
        if (button is null)
            return;

        await SetButtonArchivedAsync(button, archived: true);
    }

    private async Task UnarchiveButtonAsync()
    {
        var button = SelectedButton;
        if (button is null)
            return;

        await SetButtonArchivedAsync(button, archived: false);
    }

    private async Task DeleteButtonAsync()
    {
        var button = SelectedButton;
        if (button is null)
            return;

        IsBusy = true;
        ClearError();
        try
        {
            await _buttonService.RemoveAsync(button.Id);
            Buttons.Remove(button);
            NotifyButtonsChanged();
            SelectedButton = null;
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not delete button: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SetButtonArchivedAsync(Button button, bool archived)
    {
        IsBusy = true;
        ClearError();
        try
        {
            if (archived)
                await _buttonService.ArchiveAsync(button.Id);
            else
                await _buttonService.UnarchiveAsync(button.Id);

            await LoadButtonsAsync(button.TemplateId);
            SelectedButton = Buttons.FirstOrDefault(candidate => candidate.Id == button.Id);
            OnPropertyChanged(nameof(HasArchivedSelectedButton));
            OnPropertyChanged(nameof(HasArchivableSelectedButton));
            NotifyCommandStates();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"Could not {(archived ? "archive" : "restore")} button: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private (Color Color, float PreSeconds, float PostSeconds) ReadButtonConfiguration()
    {
        var color = ColorTranslator.FromHtml(ButtonColor);
        if (SelectedPressMode == PressMode.Double)
            return (color, 0, 0);

        return (
            color,
            ParseSeconds(PreSeconds, nameof(PreSeconds)),
            ParseSeconds(PostSeconds, nameof(PostSeconds)));
    }

    private static float ParseSeconds(string value, string fieldName)
    {
        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            || !float.IsFinite(seconds)
            || seconds < 0)
        {
            throw new ArgumentException($"{fieldName} must be a non-negative number of seconds.");
        }

        return seconds;
    }

    private void SelectButton(object? parameter)
    {
        if (parameter is Button button)
            SelectedButton = button;
    }

    private void PopulateButtonEditor(Button button)
    {
        ButtonName = button.Name ?? string.Empty;
        ButtonColor = ColorTranslator.ToHtml(button.Color);
        SelectedButtonType = button.Type;
        SelectedPressMode = button.Mode;
        Hotkey = button.HotKey ?? string.Empty;
        Note = button.Note ?? string.Empty;
        PreSeconds = button.PreSec.ToString(CultureInfo.InvariantCulture);
        PostSeconds = button.PostSec.ToString(CultureInfo.InvariantCulture);
    }

    private void ResetButtonEditor()
    {
        ButtonName = string.Empty;
        ButtonColor = "#55B7A8";
        SelectedButtonType = ButtonType.Event;
        SelectedPressMode = PressMode.Single;
        Hotkey = string.Empty;
        Note = string.Empty;
        PreSeconds = "0";
        PostSeconds = "0";
    }

    private static ButtonConfiguration CaptureConfiguration(Button button) =>
        new(button.Name ?? string.Empty, button.Color, button.Type, button.Mode, button.HotKey, button.Note, button.PreSec, button.PostSec);

    private static void RestoreConfiguration(Button button, ButtonConfiguration configuration) =>
        button.UpdateConfiguration(
            configuration.Name,
            configuration.Color,
            configuration.Type,
            configuration.Mode,
            configuration.Hotkey,
            configuration.Note,
            configuration.PreSeconds,
            configuration.PostSeconds);

    private bool CanCreateButton() =>
        !IsBusy && SelectedTemplate is not null && SelectedButton is null && !string.IsNullOrWhiteSpace(ButtonName);

    private bool CanUpdateButton() =>
        !IsBusy && SelectedTemplate is not null && SelectedButton is { Archived: false } && !string.IsNullOrWhiteSpace(ButtonName);

    private void NotifyTemplatesChanged()
    {
        OnPropertyChanged(nameof(HasTemplates));
        OnPropertyChanged(nameof(HasNoTemplates));
    }

    private void NotifyButtonsChanged()
    {
        OnPropertyChanged(nameof(HasButtons));
        OnPropertyChanged(nameof(HasNoButtons));
    }

    private void NotifyCommandStates()
    {
        foreach (var command in new ICommand[]
        {
            CreateTemplateCommand, UpdateTemplateCommand, DeleteTemplateCommand, RefreshCommand,
            CreateButtonCommand, UpdateButtonCommand, ArchiveButtonCommand, UnarchiveButtonCommand,
            DeleteButtonCommand, SelectButtonCommand, NewButtonCommand
        })
        {
            if (command is AsyncCommand asyncCommand)
                asyncCommand.NotifyCanExecuteChanged();
            else if (command is RelayCommand relayCommand)
                relayCommand.NotifyCanExecuteChanged();
        }
    }

    private static string? NormalizeOptional(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private void ClearError() => ErrorMessage = null;

    private static void RefreshCollectionItem<T>(ObservableCollection<T> collection, T item)
    {
        var index = collection.IndexOf(item);
        if (index >= 0)
        {
            collection.RemoveAt(index);
            collection.Insert(index, item);
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record ButtonConfiguration(
        string Name,
        Color Color,
        ButtonType Type,
        PressMode Mode,
        string? Hotkey,
        string? Note,
        float PreSeconds,
        float PostSeconds);

    private sealed class AsyncCommand(Func<Task> execute, Func<bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute();

        public async void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                await execute();
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    private sealed class RelayCommand(Action<object?> execute, Func<object?, bool> canExecute) : ICommand
    {
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => canExecute(parameter);

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter))
                execute(parameter);
        }

        public void NotifyCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
